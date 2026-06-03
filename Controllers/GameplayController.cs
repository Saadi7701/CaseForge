using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Core.Entities;
using CaseForgeAI.Infrastructure.Data;
using CaseForgeAI.Services;

namespace CaseForgeAI.Controllers
{
    [Authorize]
    public class GameplayController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GameplayService _gameplayService;

        public GameplayController(ApplicationDbContext context, GameplayService gameplayService)
        {
            _context = context;
            _gameplayService = gameplayService;
        }

        public async Task<IActionResult> StartCase(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var progress = await _gameplayService.StartCaseAsync(id, userId);
            
            if (progress.IsCompleted) 
                return RedirectToAction("Outcome", new { id = progress.Id });

            return RedirectToAction("Investigation", new { id = progress.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Investigation(Guid id)
        {
            var progress = await GetProgressWithRelationsAsync(id);
            if (progress == null) return NotFound();
            if (progress.IsCompleted) return RedirectToAction("Outcome", new { id = id });

            // Set current stage
            progress.CurrentStage = "Investigation";
            progress.LastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return View(progress);
        }

        [HttpPost]
        public async Task<IActionResult> CollectClueJson(Guid progressId, Guid clueId)
        {
            bool success = await _gameplayService.CollectClueAsync(progressId, clueId);
            if (success)
            {
                var clue = await _context.Clues.FindAsync(clueId);
                return Json(new { success = true, name = clue?.Name, description = clue?.Description });
            }
            return Json(new { success = false, message = "Could not collect clue." });
        }

        [HttpGet]
        public async Task<IActionResult> Interrogation(Guid id)
        {
            var progress = await GetProgressWithRelationsAsync(id);
            if (progress == null) return NotFound();
            if (progress.IsCompleted) return RedirectToAction("Outcome", new { id = id });

            progress.CurrentStage = "Interrogation";
            progress.LastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return View(progress);
        }

        [HttpPost]
        public async Task<IActionResult> InterrogateSuspectJson(Guid progressId, Guid suspectId)
        {
            var progress = await _context.PlayerProgresses.FindAsync(progressId);
            if (progress == null) return Json(new { success = false, message = "Session not found." });

            var suspect = await _context.Suspects.FindAsync(suspectId);
            if (suspect == null || suspect.StoryId != progress.StoryId) 
                return Json(new { success = false, message = "Suspect not found." });

            // Polymorphism: Call Suspect's Speak() override
            string dialogue = suspect.Speak();

            return Json(new { success = true, name = suspect.Name, dialogue = dialogue, motive = suspect.Motive });
        }

        [HttpGet]
        public async Task<IActionResult> EvidenceBoard(Guid id)
        {
            var progress = await GetProgressWithRelationsAsync(id);
            if (progress == null) return NotFound();
            if (progress.IsCompleted) return RedirectToAction("Outcome", new { id = id });

            progress.CurrentStage = "EvidenceBoard";
            progress.LastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return View(progress);
        }

        [HttpPost]
        public async Task<IActionResult> LinkCluesJson(Guid progressId, Guid clueA, Guid clueB, string notes)
        {
            bool success = await _gameplayService.AddEvidenceLinkAsync(progressId, clueA, clueB, notes);
            if (success)
            {
                var link = await _context.EvidenceLinks
                    .Include(l => l.ClueA)
                    .Include(l => l.ClueB)
                    .FirstOrDefaultAsync(l => l.PlayerProgressId == progressId && 
                        ((l.ClueIdA == clueA && l.ClueIdB == clueB) || (l.ClueIdA == clueB && l.ClueIdB == clueA)));

                return Json(new { success = true, linkId = link?.Id, clueAName = link?.ClueA?.Name, clueBName = link?.ClueB?.Name });
            }
            return Json(new { success = false, message = "Failed to establish link." });
        }

        [HttpPost]
        public async Task<IActionResult> UnlinkClueJson(Guid progressId, Guid linkId)
        {
            bool success = await _gameplayService.RemoveEvidenceLinkAsync(progressId, linkId);
            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> Puzzles(Guid id)
        {
            var progress = await GetProgressWithRelationsAsync(id);
            if (progress == null) return NotFound();
            if (progress.IsCompleted) return RedirectToAction("Outcome", new { id = id });

            progress.CurrentStage = "Puzzles";
            progress.LastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return View(progress);
        }

        [HttpPost]
        public async Task<IActionResult> SolvePuzzleJson(Guid progressId, Guid puzzleId, string answer)
        {
            bool solved = await _gameplayService.SolvePuzzleAsync(progressId, puzzleId, answer);
            return Json(new { success = solved, score = _context.PlayerProgresses.Find(progressId)?.Score });
        }

        [HttpPost]
        public async Task<IActionResult> UseHintJson(Guid progressId)
        {
            bool success = await _gameplayService.UseHintAsync(progressId);
            return Json(new { success, score = _context.PlayerProgresses.Find(progressId)?.Score });
        }

        [HttpGet]
        public async Task<IActionResult> Accuse(Guid id)
        {
            var progress = await GetProgressWithRelationsAsync(id);
            if (progress == null) return NotFound();
            if (progress.IsCompleted) return RedirectToAction("Outcome", new { id = id });

            progress.CurrentStage = "Accusation";
            progress.LastSavedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return View(progress);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAccusation(Guid progressId, Guid suspectId)
        {
            var result = await _gameplayService.AccuseSuspectAsync(progressId, suspectId);
            TempData["AccusationSuccess"] = result.Success;
            TempData["OutcomeMessage"] = result.Message;
            TempData["OutcomeDetails"] = result.Details;

            return RedirectToAction("Outcome", new { id = progressId });
        }

        [HttpGet]
        public async Task<IActionResult> Outcome(Guid id)
        {
            var progress = await _context.PlayerProgresses
                .Include(p => p.Story)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (progress == null) return NotFound();

            return View(progress);
        }

        private async Task<PlayerProgress?> GetProgressWithRelationsAsync(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.PlayerProgresses
                .Include(p => p.Story)
                .ThenInclude(s => s!.Suspects)
                .Include(p => p.Story)
                .ThenInclude(s => s!.Clues)
                .Include(p => p.Story)
                .ThenInclude(s => s!.Puzzles)
                .Include(p => p.Inventory)
                .ThenInclude(i => i.Clue)
                .Include(p => p.EvidenceLinks)
                .ThenInclude(l => l.ClueA)
                .Include(p => p.EvidenceLinks)
                .ThenInclude(l => l.ClueB)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        }
    }
}
