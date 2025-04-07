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

        // Admin: List bikes
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
                return View(await _context.Bikes.ToListAsync());
            return RedirectToAction("UserDashboard", "Account");

        }

        // Admin: Create Bike
        // GET: Bike/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Unauthorized();

            return View();
        }

        // POST: Bikes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bike bike, IFormFile imageFile)
        {
             

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
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Unauthorized();

            if (id == null) return NotFound();

            var bike = await _context.Bikes.FindAsync(id);
            return bike == null ? NotFound() : View(bike);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bike bike)
        {
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
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Unauthorized();

            var bike = await _context.Bikes.FindAsync(id);
            return bike == null ? NotFound() : View(bike);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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
        public async Task<IActionResult> UserDashboard()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var bikes = await _context.Bikes.ToListAsync();


            var userLogs = await _context.RentalLogs
                .Where(l => l.UserEmail == email && l.Action == "Approved")
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var recommended = BikeRecommender.RecommendForUser(userLogs, bikes, top: 3);
            ViewBag.Recommended = recommended;

            return View("~/Views/User/UserDashboard.cshtml", bikes);
        }



        // USER: Rent a bike  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(int BikeID, DateTime StartDateTime, int DurationHours)
        {
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
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var allLogs = await _context.RentalLogs
                .Where(l => l.Action == "Approved"||l.Action=="Returned")
                .ToListAsync();

            var availableBikes = await _context.Bikes
                .Where(b => b.AvailabilityStatus == "Available")
                .ToListAsync();

            var recommended = CollaborativeRecommender.RecommendForUser(email, allLogs, availableBikes, 3);
            return View("~/Views/User/Recommended.cshtml", recommended);
        }



    }
}
