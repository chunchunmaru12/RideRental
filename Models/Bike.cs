using System.ComponentModel.DataAnnotations;
namespace RideRental.Models
{
     public class Bike
    {
        [Key]
        public int BikeID { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string EngineType { get; set; }

        [Required]
        public string Power { get; set; }

        [Required]
        public string ColorOptions { get; set; }

        [Required]
        public string AvailabilityStatus { get; set; } = "Available";

        [Required]
        public decimal RentalPricePerHour { get; set; }

        public string ImageURL { get; set; }
    }
}

