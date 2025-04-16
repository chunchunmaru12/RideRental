using Microsoft.AspNetCore.Mvc;
using RideRental.Data;
using Microsoft.EntityFrameworkCore;
using RideRental.Models;

namespace RideRental.Controllers
{
    public class RentalRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RentalRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }
        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail"));
        }
        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.HasPendingRequests = await _context.RentalRequests.AnyAsync(r => r.Status == "Pending");
            var requests = await _context.RentalRequests
                .Include(r => r.Bike)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");

            var request = await _context.RentalRequests
                .Include(r => r.Bike)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null) return NotFound();

            //  Check if the bike is already rented
            if (request.Bike.AvailabilityStatus == "Rented")
            {
                TempData["Error"] = "This bike has already been rented by another user.";
                return RedirectToAction("Index");
            }

            request.Status = "Approved";
            request.Bike.AvailabilityStatus = "Rented";

            _context.RentalLogs.Add(new RentalLog
            {
                UserEmail = request.UserEmail,
                Action = "Approved",
                Details = $"Approved rental for {request.Bike.Model}",
                BikeModel = request.Bike.Model,
                Category = request.Bike.Category,
                EngineType = request.Bike.EngineType,
                Power = request.Bike.Power,
                ColorOptions = request.Bike.ColorOptions
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rental request approved!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var request = await _context.RentalRequests
                .Include(r => r.Bike)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null) return NotFound();

            _context.RentalLogs.Add(new RentalLog
            {
                UserEmail = request.UserEmail,
                Action = "Rejected",
                Details = $"Rejected rental for {request.Bike.Model}",
                BikeModel = request.Bike.Model,
                Category = request.Bike.Category,
                EngineType = request.Bike.EngineType,
                Power = request.Bike.Power,
                ColorOptions = request.Bike.ColorOptions
            });

            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rental request rejected.";
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Logs()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var logs = await _context.RentalLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var chartData = logs
                .GroupBy(l => new { Date = l.Timestamp.Date, l.Action })
                .Select(g => new
                {
                    Date = g.Key.Date.ToString("yyyy-MM-dd"),
                    Action = g.Key.Action,
                    Count = g.Count()
                })
                .ToList();

            ViewBag.ChartData = chartData;
            return View("~/Views/RentalRequests/RentalLogs.cshtml", logs);
        }

        //return 
        [HttpPost]
        public async Task<IActionResult> Return(int id)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Account");
            var request = await _context.RentalRequests.Include(r => r.Bike).FirstOrDefaultAsync(r => r.RequestID == id);
            if (request == null || request.Status != "Approved") return NotFound();

            request.Status = "Returned";
            request.Bike.AvailabilityStatus = "Available";

            _context.RentalLogs.Add(new RentalLog
            {
                UserEmail = request.UserEmail,
                Action = "Returned",
                Details = $"Returned rental for {request.Bike.Model}",
                BikeModel = request.Bike.Model,
                Category = request.Bike.Category,
                EngineType = request.Bike.EngineType,
                Power = request.Bike.Power,
                ColorOptions = request.Bike.ColorOptions
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Bike returned successfully!";
            return RedirectToAction("Index");
        }



    }

}
