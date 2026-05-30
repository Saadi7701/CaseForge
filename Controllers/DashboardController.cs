using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseForgeAI.Infrastructure.Data;

namespace CaseForgeAI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalCases = await _context.Stories.CountAsync();
            var publishedCases = await _context.Stories.CountAsync(s => s.IsPublished);
            var totalPlays = await _context.Analytics.SumAsync(a => a.TotalPlays);
            var solvedPlays = await _context.Analytics.SumAsync(a => a.SolvedCount);

            ViewBag.TotalCases = totalCases;
            ViewBag.PublishedCases = publishedCases;
            ViewBag.TotalPlays = totalPlays;
            ViewBag.SolvedPlays = solvedPlays;

            // Activity Log (recent progress sessions)
            var activityLog = await _context.PlayerProgresses
                .Include(p => p.Story)
                .OrderByDescending(p => p.LastSavedAt)
                .Take(5)
                .Select(p => new ActivityEntry
                {
                    UserId = p.UserId,
                    CaseTitle = p.Story != null ? p.Story.Title : "Unknown Case",
                    Stage = p.CurrentStage,
                    Time = p.LastSavedAt.ToString("g"),
                    Completed = p.IsCompleted,
                    Solved = p.CaseSolved
                })
                .ToListAsync();

            return View(activityLog);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalyticsJson()
        {
            // Query analytics for Chart.js
            var data = await _context.Analytics
                .Include(a => a.Story)
                .Where(a => a.Story != null && a.Story.IsPublished)
                .Select(a => new
                {
                    title = a.Story!.Title,
                    plays = a.TotalPlays,
                    solved = a.SolvedCount,
                    avgScore = a.AverageScore
                })
                .ToListAsync();

            return Json(data);
        }
    }

    public class ActivityEntry
    {
        public string UserId { get; set; } = string.Empty;
        public string CaseTitle { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public bool Solved { get; set; }
    }
}
