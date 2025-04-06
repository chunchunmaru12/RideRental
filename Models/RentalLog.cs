using System.ComponentModel.DataAnnotations;

namespace RideRental.Models
{
    public class RentalLog
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        public string UserEmail { get; set; }

        [Required]
        public string Action { get; set; } // Requested / Approved / Rejected

        [Required]
        public string Details { get; set; } // Summary (shown in UI/log)

        // for   recommendations:
        public string BikeModel { get; set; }
        public string Category { get; set; }
        public string EngineType { get; set; }
        public string Power { get; set; }
        public string ColorOptions { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
