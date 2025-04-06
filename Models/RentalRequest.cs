using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideRental.Models
{
    public class RentalRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        public int BikeID { get; set; }
        public Bike Bike { get; set; }

        [Required]
        public string UserEmail { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public int DurationHours { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;
    }
}
