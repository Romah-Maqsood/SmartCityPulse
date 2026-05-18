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
            return View(new Incident());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Incident incident)
        {
            if (ModelState.IsValid)
            {
                incident.ReportedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.Status = "Open";
                incident.Comments = new List<IncidentComment>();

                await _context.Incidents.InsertOneAsync(incident);

                TempData["SuccessMessage"] = "✅ Incident reported successfully!";
                return RedirectToAction("Index", "Home");
            }
            return View(incident);
        }

        // ==================== PUBLIC: Basic Incident List ====================
        public async Task<IActionResult> Index()
        {
            var incidents = await _context.Incidents.Find(_ => true).ToListAsync();
            return View(incidents);
        }
    }
}