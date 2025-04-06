
using Microsoft.EntityFrameworkCore;
using RideRental.Models;
using System.Collections.Generic;
namespace RideRental.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Bike> Bikes { get; set; }
        public DbSet<RentalRequest> RentalRequests { get; set; }

        public DbSet<RentalLog> RentalLogs { get; set; }


    }

}
