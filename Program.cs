using Microsoft.EntityFrameworkCore;
using RideRental.Data;
using RideRental.Models;
using Microsoft.AspNetCore.Identity;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
 

builder.Services.AddSession(); // Enables session
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();



// Apply migrations and seed Admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            FullName = "Admin",
            Age = 0,
            Occupation = "Other",
            Email = "admin@riderental.com",
            Role = "Admin"
        };

        // Hash the password before storing
        admin.Password = hasher.HashPassword(admin, "admin123");

        context.Users.Add(admin);
        context.SaveChanges();
    }
}


// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serves static files from wwwroot
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
