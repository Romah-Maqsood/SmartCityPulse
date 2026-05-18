using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;
using System.Text;

namespace SmartCityPulse.Controllers
{
    public class AdminController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(MongoDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
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

            try
            {
                var todayStart = DateTime.UtcNow.Date;
                var todayEnd = todayStart.AddDays(1);

                var totalToday = await _context.Incidents
                    .CountDocumentsAsync(i => i.ReportedAt >= todayStart && i.ReportedAt < todayEnd);
                var resolvedToday = await _context.Incidents
                    .CountDocumentsAsync(i => i.Status == "Resolved" && i.UpdatedAt >= todayStart && i.UpdatedAt < todayEnd);
                var criticalIncidents = await _context.Incidents
                    .CountDocumentsAsync(i => i.Severity == "Critical" && i.Status != "Resolved");
                var pendingIncidents = await _context.Incidents
                    .CountDocumentsAsync(i => i.Status == "Open" || i.Status == "In Progress");

                var recentIncidents = await _context.Incidents
                    .Find(_ => true)
                    .SortByDescending(i => i.ReportedAt)
                    .Limit(5)
                    .ToListAsync();

                var criticalPending = await _context.Incidents
                    .CountDocumentsAsync(i => i.Severity == "Critical" && (i.Status == "Open" || i.Status == "In Progress"));

                ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Admin";
                ViewBag.TotalToday = totalToday;
                ViewBag.ResolvedToday = resolvedToday;
                ViewBag.CriticalIncidents = criticalIncidents;
                ViewBag.PendingIncidents = pendingIncidents;
                ViewBag.RecentIncidents = recentIncidents;
                ViewBag.CriticalPending = criticalPending;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard load error");
                TempData["Error"] = "Failed to load dashboard: " + ex.Message;
                return View();
            }
        }

        // ==================== GET ALL INCIDENTS (AJAX) ====================
        [HttpGet]
        public async Task<IActionResult> GetAllIncidentsJson(string? status, string? severity)
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllIncidentsJson error");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==================== INCIDENT DETAIL ====================
        [HttpGet]
        public async Task<IActionResult> IncidentDetail(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();

            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Admin";
            return View(incident);
        }

        // ==================== OPERATOR MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> GetOperatorsJson()
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                var operators = await _context.Operators.Find(_ => true).ToListAsync();
                operators.ForEach(o => o.PasswordHash = null);
                return Json(operators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOperatorsJson error");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOperator([FromBody] AppUser newOperator)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOperator error");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOperator(string id, [FromBody] AppUser updated)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateOperator error");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOperator([FromBody] AppUser delOp)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                await _context.Operators.DeleteOneAsync(o => o.Id == delOp.Id);
                return Json(new { success = true, message = "Operator deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteOperator error");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // ==================== CITIZEN MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> GetCitizensJson()
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                var citizens = await _context.Users.Find(u => u.Role == "Citizen").ToListAsync();
                citizens.ForEach(c => c.PasswordHash = null);
                return Json(citizens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCitizensJson error");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCitizen(string id, [FromBody] AppUser updated)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                if (id != updated.Id) return Json(new { success = false, message = "Id mismatch." });

                var existing = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (existing == null) return Json(new { success = false, message = "Citizen not found." });

                existing.Name = updated.Name;
                existing.Email = updated.Email;
                existing.Phone = updated.Phone;
                if (!string.IsNullOrEmpty(updated.PasswordHash))
                    existing.PasswordHash = updated.PasswordHash;

                await _context.Users.ReplaceOneAsync(u => u.Id == id, existing);
                return Json(new { success = true, message = "Citizen updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCitizen error");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCitizen([FromBody] AppUser delCitizen)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var currentUserId = HttpContext.Session.GetString("UserId");
                if (delCitizen.Id == currentUserId)
                    return Json(new { success = false, message = "Cannot delete your own account." });

                await _context.Users.DeleteOneAsync(u => u.Id == delCitizen.Id);
                return Json(new { success = true, message = "Citizen deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCitizen error");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // ==================== ANALYTICS ====================
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsJson()
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAnalyticsJson error");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==================== REPORT EXPORT (CSV) ====================
        [HttpGet]
        public async Task<IActionResult> ExportReport(string startDate, string endDate, string department, string severity)
        {
            if (!IsAdmin()) return Unauthorized();

            try
            {
                var incidents = await _context.Incidents.Find(_ => true).ToListAsync();

                if (DateTime.TryParse(startDate, out var start))
                    incidents = incidents.Where(i => i.ReportedAt >= start).ToList();
                if (DateTime.TryParse(endDate, out var end))
                    incidents = incidents.Where(i => i.ReportedAt <= end).ToList();
                if (!string.IsNullOrEmpty(department))
                    incidents = incidents.Where(i => i.Department == department).ToList();
                if (!string.IsNullOrEmpty(severity))
                    incidents = incidents.Where(i => i.Severity == severity).ToList();

                var csv = new StringBuilder();
                csv.AppendLine("ID,Title,Description,Location,Department,Severity,Status,ReportedAt,UpdatedAt");

                foreach (var inc in incidents)
                {
                    csv.AppendLine(
                        $"\"{inc.Id}\"," +
                        $"\"{(inc.Title ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(inc.Description ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(inc.Location ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(inc.Department ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{inc.Severity}\"," +
                        $"\"{inc.Status}\"," +
                        $"\"{inc.ReportedAt:yyyy-MM-dd HH:mm}\"," +
                        $"\"{inc.UpdatedAt:yyyy-MM-dd HH:mm}\""
                    );
                }

                var fileName = $"Incident_Report_{DateTime.UtcNow:yyyyMMddHHmm}.csv";
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportReport error");
                return StatusCode(500, $"Export failed: {ex.Message}");
            }
        }
    }
}