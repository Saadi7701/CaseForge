using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Infrastructure.Data;
using CaseForgeAI.Models;

namespace CaseForgeAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Retrieve published stories
            // Filter out stories that the current user has already completed
            var storiesQuery = _context.Stories
                .Where(s => s.IsPublished);

            if (!string.IsNullOrEmpty(userId))
            {
                storiesQuery = storiesQuery.Where(s => 
                    !_context.PlayerProgresses.Any(p => p.StoryId == s.Id && p.UserId == userId && p.IsCompleted)
                );
            }

            var stories = await storiesQuery
                .Include(s => s.Suspects)
                .Include(s => s.Clues)
                .Include(s => s.Puzzles)
                .Include(s => s.AnalyticsRecords)
                .ToListAsync();

            // Retrieve leaderboard (top 10 players)
            var leaderboard = await _context.Users
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
}
