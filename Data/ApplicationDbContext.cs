namespace RideRental.Data
{
    using Microsoft.EntityFrameworkCore;
    using RideRental.Models;
    using System.Collections.Generic;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Bike> Bikes { get; set; }

    }

}
