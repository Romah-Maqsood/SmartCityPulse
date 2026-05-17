using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using MongoDB.Driver;

namespace SmartCityPulse.Controllers
{
    public class IncidentController : Controller
    {
        private readonly MongoDbContext _context;

        public IncidentController(MongoDbContext context)
        {
            _context = context;
        }

        // ==================== PUBLIC: Report Incident (Citizen/Public) ====================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Incident incident)
        {
            if (ModelState.IsValid)
            {
                incident.ReportedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.Status = "Open";

                await _context.Incidents.InsertOneAsync(incident);

                TempData["SuccessMessage"] = "✅ Incident reported successfully!";
                return RedirectToAction("Index", "Home");
            }
            return View(incident);
        }

        // ==================== PUBLIC: Basic Incident List (optional) ====================
        public async Task<IActionResult> Index()
        {
            var incidents = await _context.Incidents.Find(_ => true).ToListAsync();
            return View(incidents);
        }

        // ==================== AJAX: Get Incidents as JSON (Admin Dashboard) ====================
        [HttpGet]
        public async Task<IActionResult> GetAdminIncidentsJson()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return Unauthorized();

            var incidents = await _context.Incidents.Find(_ => true)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            // Return JSON with camelCase property names for JavaScript (optional, but helpful)
            return Json(incidents);
        }

        // ==================== AJAX: Update Incident ====================
        [HttpPost]
        public async Task<IActionResult> UpdateIncident(string id, Incident updated)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin" && role != "Operator")
                return Json(new { success = false, message = "Unauthorized" });

            if (id != updated.Id) return BadRequest();

            var existing = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (existing == null) return NotFound();

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.Location = updated.Location;
            existing.Severity = updated.Severity;
            existing.Department = updated.Department;
            existing.Status = updated.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.Incidents.ReplaceOneAsync(i => i.Id == id, existing);
            return Json(new { success = true, message = "Incident updated successfully!" });
        }

        // ==================== AJAX: Delete Incident ====================
        [HttpPost]
        public async Task<IActionResult> DeleteIncident(string id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return Json(new { success = false, message = "Unauthorized" });

            await _context.Incidents.DeleteOneAsync(i => i.Id == id);
            return Json(new { success = true, message = "Deleted!" });
        }
    }
}