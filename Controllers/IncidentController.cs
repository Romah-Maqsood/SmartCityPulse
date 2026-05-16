using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Models;
using SmartCityPulse.Data;
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

        // Show Create Incident Form
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Save Incident to Database
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

        // Show all incidents (for operators/admin)
        public async Task<IActionResult> Index()
        {
            var incidents = await _context.Incidents.Find(_ => true).ToListAsync();
            return View(incidents);
        }
    }
}