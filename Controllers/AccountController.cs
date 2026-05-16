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

        // ==================== LOGIN ====================
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

            // Find user by email
            var user = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password!";
                return View();
            }

            // Check password (simple comparison for now - TODO: hash later)
            if (user.PasswordHash != password)
            {
                ViewBag.Error = "Invalid email or password!";
                return View();
            }

            // Store user info in session
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);

            TempData["SuccessMessage"] = $"Welcome back, {user.Name}!";

            // Redirect based on role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.Role == "Operator")
            {
                return RedirectToAction("Dashboard", "Operator");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // ==================== REGISTER (Citizen Only) ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword, string phone)
        {
            // Validations
            if (string.IsNullOrEmpty(name))
            {
                ViewBag.Error = "Name is required!";
                return View();
            }

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Email is required!";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters!";
                return View();
            }

            // Check if email already exists
            var existingUser = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered!";
                return View();
            }

            // Create new citizen user
            var newUser = new AppUser
            {
                Name = name,
                Email = email,
                PasswordHash = password, // TODO: Hash this later
                Phone = phone ?? "",
                Role = "Citizen",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.InsertOneAsync(newUser);

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        // ==================== LOGOUT ====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Logged out successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
}