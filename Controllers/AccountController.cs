using Microsoft.AspNetCore.Mvc;

namespace RideRental.Controllers
{
    using RideRental.Models;
    using RideRental.Data;
    
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Http;
    using System.IO;
    using System.Threading.Tasks;
    using System.Linq;

    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Register() => View();
        public IActionResult UserMain()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user, IFormFile licensePicture)
        {
            if (!ModelState.IsValid)
                return View(user);

            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(user);
            }

            if (licensePicture != null && licensePicture.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, licensePicture.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await licensePicture.CopyToAsync(stream);
                user.LicensePicturePath = "/uploads/" + licensePicture.FileName;
            }

            user.Role = "User";
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "Email and password are required.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);

                if (user.Role == "Admin")
                    return RedirectToAction("AllUsers", "Account");

                return RedirectToAction("UserMain", "Account");
            }

            ViewBag.Message = "Invalid credentials";
            return View();
        }


        
        public async Task<IActionResult> UserDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "User") return Unauthorized();

            var bikes = await _context.Bikes
                .Where(b => b.AvailabilityStatus == "Available")
                .ToListAsync();

            
            return View("~/Views/User/UserDashboard.cshtml", bikes);
        }


        public async Task<IActionResult> AdminDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return Unauthorized();

            var bikes = await _context.Bikes
                .Where(b => b.AvailabilityStatus == "Available")
                .ToListAsync();
            return View("~/Views/Admin/AdminDashboard.cshtml",bikes);

        }

        //users details:
        public async Task<IActionResult> AllUsers()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Unauthorized();

            var users = await _context.Users
                .Where(u => u.Role != "Admin")
                .ToListAsync();
            return View("~/Views/Account/AllUsers.cshtml", users);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
