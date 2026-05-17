using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;
using MongoDB.Bson;

namespace SmartCityPulse.Controllers
{
    public class AdminController : Controller
    {
        private readonly MongoDbContext _context;

        public AdminController(MongoDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // ==================== DASHBOARD ====================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);

            var totalToday = await _context.Incidents
                .CountAsync(i => i.ReportedAt >= todayStart && i.ReportedAt < todayEnd);
            var resolvedToday = await _context.Incidents
                .CountAsync(i => i.Status == "Resolved" && i.UpdatedAt >= todayStart && i.UpdatedAt < todayEnd);
            var criticalIncidents = await _context.Incidents
                .CountAsync(i => i.Severity == "Critical" && i.Status != "Resolved");
            var pendingIncidents = await _context.Incidents
                .CountAsync(i => i.Status == "Open" || i.Status == "In Progress");

            var recentIncidents = await _context.Incidents
                .Find(_ => true)
                .SortByDescending(i => i.ReportedAt)
                .Limit(5)
                .ToListAsync();

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.TotalToday = totalToday;
            ViewBag.ResolvedToday = resolvedToday;
            ViewBag.CriticalIncidents = criticalIncidents;
            ViewBag.PendingIncidents = pendingIncidents;
            ViewBag.RecentIncidents = recentIncidents;

            return View();
        }

        // ==================== GET ALL INCIDENTS (AJAX) ====================
        [HttpGet]
        public async Task<IActionResult> GetAllIncidentsJson(string? status, string? severity)
        {
            if (!IsAdmin()) return Unauthorized();

            var filterBuilder = Builders<Incident>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrEmpty(status))
                filter &= filterBuilder.Eq(i => i.Status, status);
            if (!string.IsNullOrEmpty(severity))
                filter &= filterBuilder.Eq(i => i.Severity, severity);

            var incidents = await _context.Incidents.Find(filter)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            return Json(incidents);
        }

        // ==================== INCIDENT DETAIL ====================
        [HttpGet]
        public async Task<IActionResult> IncidentDetail(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View(incident);
        }

        // ==================== UPDATE STATUS ====================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var update = Builders<Incident>.Update
                .Set(i => i.Status, status)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);
            TempData["Success"] = "Status updated successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }

        // ==================== DELETE INCIDENT ====================
        [HttpPost]
        public async Task<IActionResult> DeleteIncident(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            await _context.Incidents.DeleteOneAsync(i => i.Id == id);
            TempData["Success"] = "Incident deleted successfully!";
            return RedirectToAction("Index");
        }

        // ==================== OPERATOR MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> GetOperatorsJson()
        {
            if (!IsAdmin()) return Unauthorized();

            var operators = await _context.Operators.Find(_ => true).ToListAsync();
            operators.ForEach(o => o.PasswordHash = null);
            return Json(operators);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOperator([FromBody] AppUser newOperator)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrEmpty(newOperator.Name) || string.IsNullOrEmpty(newOperator.Email) ||
                string.IsNullOrEmpty(newOperator.PasswordHash))
                return Json(new { success = false, message = "Name, Email, and Password are required." });

            var userExists = await _context.Users.Find(u => u.Email == newOperator.Email).AnyAsync();
            var opExists = await _context.Operators.Find(o => o.Email == newOperator.Email).AnyAsync();
            if (userExists || opExists)
                return Json(new { success = false, message = "Email already registered." });

            newOperator.Role = "Operator";
            newOperator.CreatedAt = DateTime.UtcNow;

            await _context.Operators.InsertOneAsync(newOperator);
            return Json(new { success = true, message = "Operator added successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOperator(string id, [FromBody] AppUser updated)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            if (id != updated.Id) return Json(new { success = false, message = "Id mismatch." });

            var existing = await _context.Operators.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existing == null) return Json(new { success = false, message = "Operator not found." });

            existing.Name = updated.Name;
            existing.Email = updated.Email;
            existing.Role = "Operator";
            existing.Phone = updated.Phone;
            existing.Department = updated.Department;
            if (!string.IsNullOrEmpty(updated.PasswordHash))
                existing.PasswordHash = updated.PasswordHash;

            await _context.Operators.ReplaceOneAsync(o => o.Id == id, existing);
            return Json(new { success = true, message = "Operator updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOperator(string id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            await _context.Operators.DeleteOneAsync(o => o.Id == id);
            return Json(new { success = true, message = "Operator deleted." });
        }

        // ==================== ANALYTICS ====================
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsJson()
        {
            if (!IsAdmin()) return Unauthorized();

            var incidents = await _context.Incidents.Find(_ => true).ToListAsync();

            var severity = new
            {
                Critical = incidents.Count(i => i.Severity == "Critical"),
                High = incidents.Count(i => i.Severity == "High"),
                Medium = incidents.Count(i => i.Severity == "Medium"),
                Low = incidents.Count(i => i.Severity == "Low")
            };

            var deptGroups = incidents
                .GroupBy(i => i.Department ?? "Unassigned")
                .Select(g => new { Department = g.Key, Count = g.Count() });

            var last7Days = Enumerable.Range(0, 7)
                .Select(offset => DateTime.UtcNow.Date.AddDays(-offset))
                .Reverse();
            var trend = last7Days.Select(day => new
            {
                Date = day.ToString("MMM dd"),
                Total = incidents.Count(i => i.ReportedAt.Date == day),
                Resolved = incidents.Count(i => i.Status == "Resolved" && i.UpdatedAt.Date == day)
            });

            return Json(new { severity, departments = deptGroups, trend });
        }
    }
}