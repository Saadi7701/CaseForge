using System;
using System.Diagnostics;
using System.Linq;
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
            // Retrieve published stories
            var stories = await _context.Stories
                .Where(s => s.IsPublished)
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
