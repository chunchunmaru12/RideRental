using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideRental.Data;
using RideRental.Models;
using RideRental.Services;

namespace RideRental.Controllers
{
    public class BikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BikesController(ApplicationDbContext context)
        {
            _context = context;
        }
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail"));
        }
        // Admin: List bikes
        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            if (HttpContext.Session.GetString("UserRole") == "Admin")
                return View(await _context.Bikes.ToListAsync());
            return RedirectToAction("UserDashboard", "Account");

        }
        

        // Admin: Create Bike
        // GET: Bike/Create
        public IActionResult Create()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
             

            return View();
        }

        // POST: Bikes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bike bike, IFormFile imageFile)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                bike.ImageURL = "/uploads/" + fileName;
            }

            _context.Bikes.Add(bike);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bike added!";
            return RedirectToAction(nameof(Index));
        }



        // Admin: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
             

            if (id == null) return NotFound();

            var bike = await _context.Bikes.FindAsync(id);
            return bike == null ? NotFound() : View(bike);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bike bike)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            if (id != bike.BikeID)
                return NotFound();

            var existingBike = await _context.Bikes.AsNoTracking().FirstOrDefaultAsync(b => b.BikeID == id);
            if (existingBike == null)
                return NotFound();
            try
            {
                if (string.IsNullOrEmpty(bike.ImageURL))
                    bike.ImageURL = existingBike.ImageURL;
                _context.Update(bike);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bike updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Bikes.Any(b => b.BikeID == id))
                    return NotFound();

                throw;
            }
        }

        // Admin: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
             

            var bike = await _context.Bikes.FindAsync(id);
            return bike == null ? NotFound() : View(bike);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var bike = await _context.Bikes.FindAsync(id);
            if (bike != null)
            {
                _context.Bikes.Remove(bike);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Bike deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }


        // USER: Browse Available Bikes
        public async Task<IActionResult> UserDashboard(int page = 1, int pageSize = 6, string searchModel = "")
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");

            var email = HttpContext.Session.GetString("UserEmail");

            var allLogs = await _context.RentalLogs
                .Where(l => l.Action == "Approved" || l.Action == "Returned")
                .ToListAsync();

            var userLogs = allLogs.Where(l => l.UserEmail == email).ToList();

            var bikes = await _context.Bikes
                .OrderBy(b => b.Model) // Required for binary search
                .ToListAsync();

            var availableBikes = bikes.Where(b => b.AvailabilityStatus == "Available").ToList();

            //  RECOMMENDATION  
            List<Bike> recommended;
            if (userLogs.Any())
            {
                recommended = ContentBasedRecommender.RecommendFromUserLogs(userLogs, availableBikes, 3);
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    var pseudoLog = new RentalLog
                    {
                        Category = user.PreferredCategory,
                        EngineType = user.PreferredEngineType,
                        Power = user.PreferredMinPower?.ToString() ?? "0",
                        BikeModel = "PreferenceBasedModel"
                    };

                    recommended = ContentBasedRecommender.RecommendForUserFromLog(pseudoLog, availableBikes, 3);
                }
                else
                {
                    recommended = new();
                }
            }
            ViewBag.Recommended = recommended;
            ViewBag.SearchQuery = searchModel;

            //   SEARCH (if user searches by Model)
            List<Bike> filteredBikes = bikes;

            if (!string.IsNullOrEmpty(searchModel))
            {
                filteredBikes = bikes
                    .Where(b => b.Model != null && b.Model.Contains(searchModel, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }


            //   PAGINATION
            var pagedBikes = filteredBikes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(filteredBikes.Count / (double)pageSize);

            return View("~/Views/User/UserDashboard.cshtml", pagedBikes);
        }



        public async Task<IActionResult> Details(int id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var bike = await _context.Bikes.FirstOrDefaultAsync(b => b.BikeID == id);
            if (bike == null)
            {
                return View("NotFound");
            }
            return View(bike);
        }





        // USER: Rent a bike  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(int BikeID, DateTime StartDateTime, int DurationHours)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "User") return Unauthorized();
            if (StartDateTime < DateTime.Now)
            {
                TempData["Error"] = "Start date and time cannot be in the past.";
                return RedirectToAction("UserDashboard");
            }

            var hasActive = await _context.RentalRequests.AnyAsync(r =>
                r.UserEmail == userEmail &&
                (r.Status == "Pending" || r.Status == "Approved"));

            if (hasActive)
            {
                TempData["Error"] = "You already have a pending or approved request.";
                return RedirectToAction("UserDashboard");
            }

            var bike = await _context.Bikes.FindAsync(BikeID);
            if (bike == null || bike.AvailabilityStatus != "Available")
                return NotFound();

            var request = new RentalRequest
            {
                BikeID = BikeID,
                UserEmail = userEmail,
                StartDateTime = StartDateTime,
                DurationHours = DurationHours
            };
            _context.RentalLogs.Add(new RentalLog
            {
                UserEmail = userEmail,
                Action = "Requested",
                Details = $"Bike: {bike.Model}, Start: {StartDateTime}, Duration: {DurationHours}h",
                BikeModel = bike.Model,
                Category = bike.Category,
                EngineType = bike.EngineType,
                Power = bike.Power,
                ColorOptions = bike.ColorOptions
            });

            _context.RentalRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rental request submitted.";
            return RedirectToAction("UserDashboard");
        }
        //history for retals
        public async Task<IActionResult> RentalHistory()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var requests = await _context.RentalRequests
                .Include(r => r.Bike)
                .Where(r => r.UserEmail == email)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View("~/Views/User/RentalHistory.cshtml", requests);
        }
        //recommendation:
        public async Task<IActionResult> Recommended()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");

            var email = HttpContext.Session.GetString("UserEmail");
            var allLogs = await _context.RentalLogs
                .Where(l => l.Action == "Approved" || l.Action == "Returned")
                .ToListAsync();

            var availableBikes = await _context.Bikes
                .Where(b => b.AvailabilityStatus == "Available")
                .ToListAsync();

            // Check if user has rental history
            var userLogs = allLogs
                .Where(l => l.UserEmail == email)
                .ToList();

            List<Bike> recommended;

            if (userLogs.Any())
            {
                recommended = ContentBasedRecommender.RecommendFromUserLogs(userLogs, availableBikes, 3);
            }
            else
            {
                // Use saved preferences if no logs
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    var pseudoLog = new RentalLog
                    {
                        Category = user.PreferredCategory,
                        EngineType = user.PreferredEngineType,
                        Power = user.PreferredMinPower?.ToString() ?? "0",
                        BikeModel = "PreferenceBasedModel"
                    };

                    recommended = ContentBasedRecommender.RecommendForUserFromLog(pseudoLog, availableBikes, 3);
                }
                else
                {
                    recommended = new(); // Fallback empty
                }
            }

            return View("~/Views/User/Recommended.cshtml", recommended);
        }

        public async Task<IActionResult> RecommendBySimilarUsers()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var allUsers = await _context.Users.ToListAsync();
            var allLogs = await _context.RentalLogs.ToListAsync();
            var availableBikes = await _context.Bikes.Where(b => b.AvailabilityStatus == "Available").ToListAsync();

            var recommended = CollaborativeRecommender.RecommendBySimilarUsers(userEmail, allUsers, allLogs, availableBikes);
            ViewBag.Recommended = recommended;

            return View("~/Views/User/Recommended.cshtml", recommended);
        }


    }
}
