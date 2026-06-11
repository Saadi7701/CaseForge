using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Infrastructure.Data;
using CaseForgeAI.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
namespace CaseForgeAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public HomeController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [ResponseCache(Duration = 15, Location = ResponseCacheLocation.Client, NoStore = false)]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Retrieve published stories
            var storiesQuery = _context.Stories.Where(s => s.IsPublished);

            if (!string.IsNullOrEmpty(userId))
            {
                storiesQuery = storiesQuery.Where(s =>
                    !_context.PlayerProgresses.Any(p => p.StoryId == s.Id && p.UserId == userId && p.IsCompleted));
            }

            var stories = await storiesQuery
                .Include(s => s.Suspects)
                .Include(s => s.Clues)
                .Include(s => s.Puzzles)
                .Include(s => s.AnalyticsRecords)
                .ToListAsync();

            // Cache leaderboard for a short period (configurable)
            var cacheKey = "leaderboard_top10";
            if (!_cache.TryGetValue(cacheKey, out List<LeaderboardEntry> leaderboard))
            {
                leaderboard = await _context.Users
                    .OrderByDescending(u => u.TotalScore)
                    .Take(10)
                    .Select(u => new LeaderboardEntry
                    {
                        FullName = u.FullName,
                        Email = u.Email ?? "Anonymous",
                        Score = u.TotalScore,
                        Rank = u.DetectiveRank
                    })
                    .ToListAsync();


                // Use configuration value
                var configSeconds = 30; // default if config read fails
                // We'll read from configuration later; for now use default 30 seconds
                _cache.Set(cacheKey, leaderboard, TimeSpan.FromSeconds(configSeconds));
            }

            var viewModel = new HomeViewModel
            {
                Stories = stories,
                Leaderboard = leaderboard
            };

            return View(viewModel);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var completedProgress = await _context.PlayerProgresses
                .Include(p => p.Story)
                .Where(p => p.UserId == userId && p.IsCompleted)
                .OrderByDescending(p => p.LastSavedAt)
                .ToListAsync();

            return View(completedProgress);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null) return NotFound();

            var allProgress = await _context.PlayerProgresses
                .Include(p => p.Story)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var attempted = allProgress.Count;
            var solved = allProgress.Count(p => p.CaseSolved);
            var accuracy = attempted > 0 ? (double)solved / attempted * 100 : 0;

            var recentActivity = allProgress
                .Where(p => p.IsCompleted)
                .OrderByDescending(p => p.LastSavedAt)
                .Take(5)
                .ToList();

            var viewModel = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "Unknown",
                DetectiveRank = user.DetectiveRank,
                TotalScore = user.TotalScore,
                TotalCasesAttempted = attempted,
                TotalCasesSolved = solved,
                AccuracyPercentage = Math.Round(accuracy, 1),
                RecentActivity = recentActivity
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }

    public class HomeViewModel
    {
        public System.Collections.Generic.List<CaseForgeAI.Core.Entities.CaseStory> Stories { get; set; } = new();
        public System.Collections.Generic.List<LeaderboardEntry> Leaderboard { get; set; } = new();
    }

    public class LeaderboardEntry
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Rank { get; set; } = string.Empty;
    }

    public class UserProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DetectiveRank { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int TotalCasesAttempted { get; set; }
        public int TotalCasesSolved { get; set; }
        public double AccuracyPercentage { get; set; }
        public System.Collections.Generic.List<CaseForgeAI.Core.Entities.PlayerProgress> RecentActivity { get; set; } = new();
    }
}
