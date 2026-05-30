using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Core.Entities;
using CaseForgeAI.Core.Interfaces;
using CaseForgeAI.Infrastructure.Data;

namespace CaseForgeAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;

        public AdminController(ApplicationDbContext context, IAIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public async Task<IActionResult> Index()
        {
            var cases = await _context.Stories
                .Include(s => s.Versions)
                .Include(s => s.AnalyticsRecords)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(cases);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCase(string prompt, string model)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                TempData["ErrorMessage"] = "Prompt cannot be empty.";
                return RedirectToAction("Create");
            }

            try
            {
                // 1. Generate story JSON using AIService
                string jsonResult = await _aiService.GenerateStoryJsonAsync(prompt, model);

                // 2. Parse title/victim to create a draft CaseStory
                using var doc = JsonDocument.Parse(jsonResult);
                var root = doc.RootElement;
                string title = root.GetProperty("title").GetString() ?? "AI Generated Case";
                string victim = root.GetProperty("victim").GetString() ?? "Unknown Victim";
                string difficulty = root.TryGetProperty("difficulty", out var diff) ? diff.GetString() ?? "Medium" : "Medium";
                string desc = root.TryGetProperty("crimeSceneDescription", out var d) ? d.GetString() ?? "" : "";

                var story = new CaseStory
                {
                    Title = title,
                    VictimName = victim,
                    Description = "Draft Case File. Click Review to refine and publish.",
                    CrimeSceneDescription = desc,
                    Difficulty = difficulty,
                    IsPublished = false,
                    QualityScore = _aiService.CalculateQualityScore(jsonResult)
                };

                await _context.Stories.AddAsync(story);
                await _context.SaveChangesAsync();

                // 3. Save first version
                var version = new StoryVersion
                {
                    StoryId = story.Id,
                    VersionNumber = 1,
                    ContentJson = jsonResult,
                    PromptUsed = prompt,
                    CreatedBy = User.Identity?.Name ?? "Admin"
                };

                await _context.StoryVersions.AddAsync(version);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Case draft '{title}' successfully generated!";
                return RedirectToAction("Review", new { id = story.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate case: {ex.Message}";
                return RedirectToAction("Create");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Review(Guid id)
        {
            var story = await _context.Stories
                .Include(s => s.Versions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (story == null) return NotFound();

            var latestVersion = story.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            ViewBag.LatestVersion = latestVersion;

            return View(story);
        }

        [HttpPost]
        public async Task<IActionResult> RefineCase(Guid id, string feedbackPrompt, string model)
        {
            var story = await _context.Stories
                .Include(s => s.Versions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (story == null) return NotFound();

            var latestVersion = story.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            if (latestVersion == null) return BadRequest("No base version to refine.");

            try
            {
                // Call AI Service to refine the JSON
                string refinedJson = await _aiService.RefineStoryJsonAsync(latestVersion.ContentJson, feedbackPrompt, model);

                // Add new version
                var nextVersionNum = latestVersion.VersionNumber + 1;
                var version = new StoryVersion
                {
                    StoryId = story.Id,
                    VersionNumber = nextVersionNum,
                    ContentJson = refinedJson,
                    PromptUsed = feedbackPrompt,
                    CreatedBy = User.Identity?.Name ?? "Admin"
                };

                await _context.StoryVersions.AddAsync(version);
                
                // Update basic info on draft CaseStory if changed in JSON
                using var doc = JsonDocument.Parse(refinedJson);
                var root = doc.RootElement;
                story.Title = root.GetProperty("title").GetString() ?? story.Title;
                story.VictimName = root.GetProperty("victim").GetString() ?? story.VictimName;
                story.Difficulty = root.TryGetProperty("difficulty", out var diff) ? diff.GetString() ?? story.Difficulty : story.Difficulty;
                story.CrimeSceneDescription = root.TryGetProperty("crimeSceneDescription", out var d) ? d.GetString() ?? story.CrimeSceneDescription : story.CrimeSceneDescription;
                story.QualityScore = _aiService.CalculateQualityScore(refinedJson);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Generated version {nextVersionNum} successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Refinement failed: {ex.Message}";
            }

            return RedirectToAction("Review", new { id = story.Id });
        }

        [HttpPost]
        public async Task<IActionResult> PublishCase(Guid id, Guid versionId)
        {
            var story = await _context.Stories
                .Include(s => s.Suspects)
                .Include(s => s.Clues)
                .Include(s => s.Puzzles)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (story == null) return NotFound();

            var version = await _context.StoryVersions.FindAsync(versionId);
            if (version == null || version.StoryId != id) return BadRequest("Invalid version.");

            try
            {
                // 1. Clear old suspects, clues, puzzles associated with draft (since we are rewriting/publishing)
                _context.Suspects.RemoveRange(story.Suspects);
                _context.Clues.RemoveRange(story.Clues);
                _context.Puzzles.RemoveRange(story.Puzzles);

                // 2. Parse version content JSON and map to EF models
                using var doc = JsonDocument.Parse(version.ContentJson);
                var root = doc.RootElement;

                story.Title = root.GetProperty("title").GetString() ?? story.Title;
                story.VictimName = root.GetProperty("victim").GetString() ?? story.VictimName;
                story.Description = $"Case of the murder of {story.VictimName}. Clues collected from crime scene investigation.";
                story.CrimeSceneDescription = root.TryGetProperty("crimeSceneDescription", out var csd) ? csd.GetString() ?? "" : "";
                story.Difficulty = root.TryGetProperty("difficulty", out var diff) ? diff.GetString() ?? "Medium" : "Medium";

                // Parse Suspects
                if (root.TryGetProperty("suspects", out var suspectsArray))
                {
                    foreach (var s in suspectsArray.EnumerateArray())
                    {
                        var suspect = new Suspect
                        {
                            StoryId = story.Id,
                            Name = s.GetProperty("name").GetString() ?? "Unnamed Suspect",
                            Role = s.GetProperty("role").GetString() ?? "Suspect",
                            Description = s.GetProperty("description").GetString() ?? "",
                            Alibi = s.GetProperty("alibi").GetString() ?? "",
                            Motive = s.GetProperty("motive").GetString() ?? "",
                            IsKiller = s.GetProperty("isKiller").GetBoolean(),
                            AvatarUrl = s.TryGetProperty("avatarUrl", out var av) ? av.GetString() ?? "/images/default_suspect.jpg" : "/images/default_suspect.jpg"
                        };
                        await _context.Suspects.AddAsync(suspect);
                    }
                }

                // Parse Clues
                if (root.TryGetProperty("clues", out var cluesArray))
                {
                    foreach (var c in cluesArray.EnumerateArray())
                    {
                        var clue = new Clue
                        {
                            StoryId = story.Id,
                            Name = c.GetProperty("name").GetString() ?? "Unnamed Clue",
                            Description = c.GetProperty("description").GetString() ?? "",
                            LocationName = c.GetProperty("locationName").GetString() ?? "Crime Scene",
                            ClueType = c.GetProperty("clueType").GetString() ?? "Physical",
                            IsHidden = c.TryGetProperty("isHidden", out var hid) && hid.GetBoolean(),
                            ConnectionInfo = c.TryGetProperty("connectionInfo", out var conn) ? conn.GetString() ?? "" : "",
                            HotspotX = c.TryGetProperty("hotspotX", out var xVal) ? xVal.GetInt32() : 50,
                            HotspotY = c.TryGetProperty("hotspotY", out var yVal) ? yVal.GetInt32() : 50
                        };
                        await _context.Clues.AddAsync(clue);
                    }
                }

                // Parse Puzzles
                if (root.TryGetProperty("puzzles", out var puzzlesArray))
                {
                    foreach (var p in puzzlesArray.EnumerateArray())
                    {
                        var puzzle = new PuzzleEntity
                        {
                            StoryId = story.Id,
                            Title = p.GetProperty("title").GetString() ?? "Cryptic Lock",
                            PuzzleType = p.GetProperty("puzzleType").GetString() ?? "Cipher",
                            Question = p.GetProperty("question").GetString() ?? "Solve the puzzle.",
                            CorrectAnswer = p.GetProperty("correctAnswer").GetString() ?? "",
                            Hint = p.GetProperty("hint").GetString() ?? "",
                            PointsValue = p.TryGetProperty("pointsValue", out var pts) ? pts.GetInt32() : 200,
                            PuzzleDataJson = p.TryGetProperty("puzzleDataJson", out var pData) ? pData.ToString() : "{}"
                        };
                        await _context.Puzzles.AddAsync(puzzle);
                    }
                }

                // Parse Ending
                if (root.TryGetProperty("ending", out var endStr))
                {
                    // Story version updates have ending text
                    story.Description += $" Solution ending configured.";
                }

                story.IsPublished = true;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Case '{story.Title}' is now published for players!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Publishing failed: {ex.Message}";
                return RedirectToAction("Review", new { id = story.Id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCase(Guid id)
        {
            var story = await _context.Stories.FindAsync(id);
            if (story != null)
            {
                _context.Stories.Remove(story);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Case deleted successfully.";
            }
            return RedirectToAction("Index");
        }
    }
}
