using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace SmartCityPulse.Controllers
{
    public class OperatorController : Controller
    {
        private readonly MongoDbContext _context;

        public OperatorController(MongoDbContext context)
        {
            _context = context;
        }

        private bool IsOperatorLoggedIn()
        {
            return HttpContext.Session.GetString("UserRole") == "Operator";
        }

        private string GetOperatorDepartment()
        {
            return HttpContext.Session.GetString("UserDepartment") ?? "";
        }

        private string GetOperatorName()
        {
            return HttpContext.Session.GetString("UserName") ?? "Operator";
        }

        // ==================== DASHBOARD WITH ANALYTICS ====================
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var operatorName = GetOperatorName();

            var filter = Builders<Incident>.Filter.Eq(i => i.Department, department);
            var incidents = await _context.Incidents.Find(filter).ToListAsync();

            // Analytics Data
            var today = DateTime.UtcNow.Date;

            var newToday = incidents.Count(i => i.ReportedAt.Date == today && i.Status == "Open");
            var inProgress = incidents.Count(i => i.Status == "In Progress");
            var resolved = incidents.Count(i => i.Status == "Resolved");
            var total = incidents.Count;

            // Severity Distribution
            var criticalCount = incidents.Count(i => i.Severity == "Critical");
            var highCount = incidents.Count(i => i.Severity == "High");
            var mediumCount = incidents.Count(i => i.Severity == "Medium");
            var lowCount = incidents.Count(i => i.Severity == "Low");

            // Status Distribution for Pie Chart
            var openCount = incidents.Count(i => i.Status == "Open");
            var inProgressCount = incidents.Count(i => i.Status == "In Progress");
            var resolvedCount = incidents.Count(i => i.Status == "Resolved");

            // Last 7 days trend
            var last7Days = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                last7Days.Add(incidents.Count(inc => inc.ReportedAt.Date == date));
            }

            // Monthly Trend
            var monthlyData = new Dictionary<string, int>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var monthName = month.ToString("MMM yyyy");
                monthlyData[monthName] = incidents.Count(inc =>
                    inc.ReportedAt.Year == month.Year && inc.ReportedAt.Month == month.Month);
            }

            ViewBag.OperatorName = operatorName;
            ViewBag.Department = department;
            ViewBag.NewToday = newToday;
            ViewBag.InProgress = inProgress;
            ViewBag.Resolved = resolved;
            ViewBag.Total = total;

            // For Charts
            ViewBag.CriticalCount = criticalCount;
            ViewBag.HighCount = highCount;
            ViewBag.MediumCount = mediumCount;
            ViewBag.LowCount = lowCount;
            ViewBag.OpenCount = openCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.ResolvedCount = resolvedCount;
            ViewBag.Last7Days = JsonConvert.SerializeObject(last7Days);
            ViewBag.MonthlyData = JsonConvert.SerializeObject(monthlyData);

            var viewModel = new OperatorDashboardViewModel
            {
                OperatorName = operatorName,
                Department = department,
                NewToday = newToday,
                InProgress = inProgress,
                Resolved = resolved,
                TotalIncidents = total,
                RecentIncidents = incidents.OrderByDescending(i => i.ReportedAt).Take(10).ToList()
            };

            return View(viewModel);
        }

        // ==================== ALL INCIDENTS ====================
        public async Task<IActionResult> Incidents(string? status, string? severity)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var filterBuilder = Builders<Incident>.Filter;
            var filter = filterBuilder.Eq(i => i.Department, department);

            if (!string.IsNullOrEmpty(status))
                filter &= filterBuilder.Eq(i => i.Status, status);

            if (!string.IsNullOrEmpty(severity))
                filter &= filterBuilder.Eq(i => i.Severity, severity);

            var incidents = await _context.Incidents.Find(filter)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = department;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSeverity = severity;

            return View(incidents);
        }

        // ==================== INCIDENT DETAIL ====================
        public async Task<IActionResult> IncidentDetail(string id)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();

            if (incident == null)
                return NotFound();

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = department;

            return View(incident);
        }

        // ==================== UPDATE STATUS ====================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status, string comment)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return NotFound();

            var update = Builders<Incident>.Update
                .Set(i => i.Status, status)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(comment))
            {
                var newComment = new IncidentComment
                {
                    Text = comment,
                    Author = GetOperatorName(),
                    CreatedAt = DateTime.UtcNow
                };
                incident.Comments.Add(newComment);
                update = update.Set(i => i.Comments, incident.Comments);
            }

            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);

            TempData["Success"] = "Incident updated successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }

        // ==================== CREATE INCIDENT ====================
        [HttpGet]
        public IActionResult CreateIncident()
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = GetOperatorDepartment();

            return View(new Incident());
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncident(Incident model)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                model.Department = GetOperatorDepartment();
                model.ReportedBy = GetOperatorName();
                model.ReportedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                model.Status = "Open";
                model.Comments = new List<IncidentComment>
                {
                    new IncidentComment
                    {
                        Text = $"Incident reported by {GetOperatorName()}",
                        Author = "System",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Incidents.InsertOneAsync(model);

                TempData["Success"] = "Incident created successfully!";
                return RedirectToAction("Incidents");
            }

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = GetOperatorDepartment();
            return View(model);
        }

        // ==================== ADD COMMENT ====================
        [HttpPost]
        public async Task<IActionResult> AddComment(string id, string commentText)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return NotFound();

            var newComment = new IncidentComment
            {
                Text = commentText,
                Author = GetOperatorName(),
                CreatedAt = DateTime.UtcNow
            };

            incident.Comments.Add(newComment);

            var update = Builders<Incident>.Update
                .Set(i => i.Comments, incident.Comments)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);

            TempData["Success"] = "Comment added successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }
    }
}