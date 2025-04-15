namespace RideRental.Models
{
    using System.ComponentModel.DataAnnotations;

    public class User
    {
        public int Id { get; set; }

        [Required]
        public string? FullName { get; set; }

        public int Age { get; set; }

        [Required]
        public string Occupation { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string? LicensePicturePath { get; set; }

        public string Role { get; set; } = "User"; // or "Admin"
        public string? PreferredCategory { get; set; }
        public string? PreferredEngineType { get; set; }
        public double? PreferredMinPower { get; set; }

    }

}
