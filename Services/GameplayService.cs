using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Core.Entities;
using CaseForgeAI.Core.Entities.Identity;
using CaseForgeAI.Core.Interfaces;
using CaseForgeAI.Infrastructure.Data;

namespace CaseForgeAI.Services
{
    // Composition and Encapsulation of gameplay operations
    public class GameplayService
    {
        private readonly ApplicationDbContext _context;

        public GameplayService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlayerProgress> StartCaseAsync(Guid caseId, string userId)
        {
            // Check if progress already exists
            var existing = await _context.PlayerProgresses
                .Include(p => p.Inventory)
                .Include(p => p.EvidenceLinks)
                .FirstOrDefaultAsync(p => p.StoryId == caseId && p.UserId == userId);

            if (existing != null)
            {
                existing.LastSavedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existing;
            }

            var progress = new PlayerProgress
            {
                UserId = userId,
                StoryId = caseId,
                Score = 1000,
                CurrentStage = "Investigation",
                IsCompleted = false,
                StartedAt = DateTime.UtcNow,
                LastSavedAt = DateTime.UtcNow
            };

            await _context.PlayerProgresses.AddAsync(progress);
            await _context.SaveChangesAsync();

            // Record story play in analytics
            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.StoryId == caseId);
            if (analytics == null)
            {
                analytics = new Analytics { StoryId = caseId, TotalPlays = 1 };
                await _context.Analytics.AddAsync(analytics);
            }
            else
            {
                analytics.TotalPlays++;
                analytics.LastUpdated = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            return progress;
        }

        public async Task<bool> CollectClueAsync(Guid progressId, Guid clueId)
        {
            var progress = await _context.PlayerProgresses
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return false;

            // Check if already collected
            if (progress.Inventory.Any(i => i.ClueId == clueId))
                return true;

            var item = new InventoryItem
            {
                PlayerProgressId = progressId,
                ClueId = clueId,
                CollectedAt = DateTime.UtcNow
            };

            await _context.InventoryItems.AddAsync(item);
            
            // Increment score slightly for finding evidence
            progress.Score = Math.Min(progress.Score + 50, 1500);
            progress.LastSavedAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddEvidenceLinkAsync(Guid progressId, Guid clueA, Guid clueB, string notes)
        {
            var progress = await _context.PlayerProgresses
                .Include(p => p.EvidenceLinks)
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return false;

            // Prevent duplicate links
            var exists = progress.EvidenceLinks.Any(l => 
                (l.ClueIdA == clueA && l.ClueIdB == clueB) || 
                (l.ClueIdA == clueB && l.ClueIdB == clueA));

            if (exists) return true;

            var link = new EvidenceLink
            {
                PlayerProgressId = progressId,
                ClueIdA = clueA,
                ClueIdB = clueB,
                LinkNotes = notes,
                CreatedAt = DateTime.UtcNow
            };

            await _context.EvidenceLinks.AddAsync(link);
            progress.LastSavedAt = DateTime.UtcNow;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveEvidenceLinkAsync(Guid progressId, Guid linkId)
        {
            var link = await _context.EvidenceLinks.FindAsync(linkId);
            if (link == null || link.PlayerProgressId != progressId) return false;

            _context.EvidenceLinks.Remove(link);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SolvePuzzleAsync(Guid progressId, Guid puzzleId, string answer)
        {
            var progress = await _context.PlayerProgresses.FindAsync(progressId);
            if (progress == null) return false;

            var puzzleEntity = await _context.Puzzles.FindAsync(puzzleId);
            if (puzzleEntity == null || puzzleEntity.StoryId != progress.StoryId) return false;

            // Instantiate concrete puzzle polymorphically
            var puzzle = puzzleEntity.ToConcretePuzzle();
            bool isCorrect = puzzle.VerifyAnswer(answer);

            if (isCorrect)
            {
                progress.Score += puzzle.PointsValue;
                progress.LastSavedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Penalize score for wrong submission
                progress.Score = Math.Max(progress.Score - 50, 100);
                progress.LastSavedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return isCorrect;
        }

        public async Task<bool> UseHintAsync(Guid progressId)
        {
            var progress = await _context.PlayerProgresses.FindAsync(progressId);
            if (progress == null) return false;

            // Penalty for hint
            progress.Score = Math.Max(progress.Score - 100, 100);
            progress.LastSavedAt = DateTime.UtcNow;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<AccusationResult> AccuseSuspectAsync(Guid progressId, Guid suspectId)
        {
            var progress = await _context.PlayerProgresses
                .Include(p => p.Story)
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return new AccusationResult { Success = false, Message = "Session not found." };

            var suspect = await _context.Suspects.FindAsync(suspectId);
            if (suspect == null || suspect.StoryId != progress.StoryId) 
                return new AccusationResult { Success = false, Message = "Suspect not found." };

            progress.LastSavedAt = DateTime.UtcNow;
            progress.IsCompleted = true;

            var analytics = await _context.Analytics.FirstOrDefaultAsync(a => a.StoryId == progress.StoryId);

            if (suspect.IsKiller)
            {
                progress.CaseSolved = true;
                progress.CurrentStage = "Finished";
                
                // Add to player's global total score
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == progress.UserId);
                if (user != null)
                {
                    user.TotalScore += progress.Score;
                    // Rank promotions
                    if (user.TotalScore > 5000) user.DetectiveRank = "Master Detective";
                    else if (user.TotalScore > 2000) user.DetectiveRank = "Senior Inspector";
                }

                // Update analytics
                if (analytics != null)
                {
                    analytics.SolvedCount++;
                    double totalSecs = (DateTime.UtcNow - progress.StartedAt).TotalSeconds;
                    analytics.AverageSolveTimeSeconds = ((analytics.AverageSolveTimeSeconds * (analytics.TotalPlays - 1)) + totalSecs) / analytics.TotalPlays;
                    analytics.AverageScore = ((analytics.AverageScore * (analytics.TotalPlays - 1)) + progress.Score) / analytics.TotalPlays;
                    analytics.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return new AccusationResult
                {
                    Success = true,
                    Message = "Correct Accusation!",
                    Details = progress.Story?.Ending ?? "The suspect broke down and confessed to the crime."
                };
            }
            else
            {
                progress.CaseSolved = false;
                progress.CurrentStage = "Finished";
                progress.Score = 0; // Failed cases get 0 score

                await _context.SaveChangesAsync();

                return new AccusationResult
                {
                    Success = false,
                    Message = "Wrong Accusation!",
                    Details = $"You accused {suspect.Name}, but they had a solid alibi. The true killer escaped, leaving the department in disgrace."
                };
            }
        }
    }

    public class AccusationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
