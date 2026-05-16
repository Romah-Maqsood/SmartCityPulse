using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;

namespace SmartCityPulse.Controllers
{
    public class AdminController : Controller
    {
        private readonly MongoDbContext _context;

        public AdminController(MongoDbContext context)
        {
            _context = context;
        }

        // Check if user is admin
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            // Get today's date range
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);

            // Dashboard Stats
            var totalToday = await _context.Incidents
                .CountAsync(i => i.ReportedAt >= todayStart && i.ReportedAt < todayEnd);

            var resolvedToday = await _context.Incidents
                .CountAsync(i => i.Status == "Resolved" && i.UpdatedAt >= todayStart && i.UpdatedAt < todayEnd);

            var criticalIncidents = await _context.Incidents
                .CountAsync(i => i.Severity == "Critical" && i.Status != "Resolved");

            var pendingIncidents = await _context.Incidents
                .CountAsync(i => i.Status == "Open" || i.Status == "Pending");

            // Calculate trends (compared to yesterday)
            var yesterdayStart = todayStart.AddDays(-1);
            var yesterdayEnd = yesterdayStart.AddDays(1);
            var totalYesterday = await _context.Incidents
                .CountAsync(i => i.ReportedAt >= yesterdayStart && i.ReportedAt < yesterdayEnd);

            var totalTrend = totalYesterday > 0 ? ((totalToday - totalYesterday) * 100 / totalYesterday) : 0;
            var resolvedYesterday = await _context.Incidents
                .CountAsync(i => i.Status == "Resolved" && i.UpdatedAt >= yesterdayStart && i.UpdatedAt < yesterdayEnd);
            var resolvedTrend = resolvedYesterday > 0 ? ((resolvedToday - resolvedYesterday) * 100 / resolvedYesterday) : 0;

            // Recent Incidents (last 5)
            var recentIncidents = await _context.Incidents
                .Find(_ => true)
                .SortByDescending(i => i.ReportedAt)
                .Limit(5)
                .ToListAsync();

            // Critical Incidents Pending
            var criticalPending = await _context.Incidents
                .Find(i => i.Severity == "Critical" && (i.Status == "Open" || i.Status == "Pending"))
                .CountAsync();

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.TotalToday = totalToday;
            ViewBag.ResolvedToday = resolvedToday;
            ViewBag.CriticalIncidents = criticalIncidents;
            ViewBag.PendingIncidents = pendingIncidents;
            ViewBag.TotalTrend = totalTrend;
            ViewBag.ResolvedTrend = resolvedTrend;
            ViewBag.RecentIncidents = recentIncidents;
            ViewBag.CriticalPending = criticalPending;

            return View();
        }

        // For sidebar navigation
        public IActionResult Incidents()
        {
            return ViewComponent("IncidentsList");
        }

        public IActionResult Operators()
        {
            return ViewComponent("OperatorsList");
        }

        public IActionResult Analytics()
        {
            return ViewComponent("AnalyticsDashboard");
        }

        public IActionResult Reports()
        {
            return ViewComponent("ReportsExport");
        }
    }
}