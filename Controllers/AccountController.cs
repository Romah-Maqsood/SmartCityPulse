using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;

namespace SmartCityPulse.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbContext _context;

        public AccountController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required!";
                return View();
            }

            // Check in Users collection first
            var user = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user != null && user.PasswordHash == password)
            {
                SetSession(user);
                return RedirectByRole(user.Role);
            }

            // If not found, check in Operators collection
            var op = await _context.Operators.Find(o => o.Email == email).FirstOrDefaultAsync();
            if (op != null && op.PasswordHash == password)
            {
                SetSession(op);
                return RedirectByRole(op.Role);
            }

            ViewBag.Error = "Invalid email or password!";
            return View();
        }

        private void SetSession(AppUser user)
        {
            HttpContext.Session.SetString("UserId", user.Id ?? "");
            HttpContext.Session.SetString("UserName", user.Name ?? "");
            HttpContext.Session.SetString("UserEmail", user.Email ?? "");
            HttpContext.Session.SetString("UserRole", user.Role ?? "");
            // ✅ Department add karo
            HttpContext.Session.SetString("UserDepartment", user.Department ?? "");
        }

        private IActionResult RedirectByRole(string role)
        {
            if (role == "Admin")
                return RedirectToAction("Index", "Admin");
            else if (role == "Operator")
                return RedirectToAction("Dashboard", "Operator"); // ✅ Dashboard hai Index nahi
            else
                return RedirectToAction("Index", "Home");
        }

        // ==================== REGISTER (unchanged, only for Citizens) ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword, string phone)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || password != confirmPassword || password.Length < 6)
            {
                ViewBag.Error = "Please fill all fields correctly.";
                return View();
            }

            var existingUser = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered!";
                return View();
            }

            var newUser = new AppUser
            {
                Name = name,
                Email = email,
                PasswordHash = password,
                Phone = phone,
                Role = "Citizen",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(newUser);

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Logged out successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
}