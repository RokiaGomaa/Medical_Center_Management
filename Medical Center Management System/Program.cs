using Medical_Center_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using Medical_Center_Management_System.Data;

namespace Medical_Center_Management_System
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // =========================================
            // Database
            // =========================================
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"));
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            });

            // =========================================
            // ASP.NET Core Identity
            // =========================================
            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Password policy — adjust to taste
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;

                    // Lockout
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                    // User
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // =========================================
            // Cookie settings
            // =========================================
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

            var app = builder.Build();

            RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotative");

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Auth middleware — ORDER MATTERS: Authentication before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            //app.MapControllerRoute(
            //    name: "default",
            //    pattern: "{controller=Home}/{action=Index}/{id?}");

            // =========================================
            // Seed roles and default admin account
            //// =========================================
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await SeedData.InitializeAsync(app.Services);

            app.Run();
        }

        //private static async Task SeedAsync(WebApplication app)
        //{
        //    using var scope = app.Services.CreateScope();
        //    var roleManager = scope.ServiceProvider
        //        .GetRequiredService<RoleManager<IdentityRole>>();
        //    var userManager = scope.ServiceProvider
        //        .GetRequiredService<UserManager<ApplicationUser>>();

        //    // Create roles
        //    string[] roles = { "Admin", "Doctor", "Patient" };
        //    foreach (var role in roles)
        //    {
        //        if (!await roleManager.RoleExistsAsync(role))
        //            await roleManager.CreateAsync(new IdentityRole(role));
        //    }

        //    // Create default admin if none exists
        //    const string adminEmail = "admin@medix.com";
        //    const string adminPassword = "Admin@12345";

        //    if (await userManager.FindByEmailAsync(adminEmail) == null)
        //    {
        //        var admin = new ApplicationUser
        //        {
        //            UserName = adminEmail,
        //            Email = adminEmail,
        //            FullName = "System Administrator",
        //            EmailConfirmed = true
        //        };

        //        var result = await userManager.CreateAsync(admin, adminPassword);

        //        if (result.Succeeded)
        //            await userManager.AddToRoleAsync(admin, "Admin");
        //    }
        //}
    }
}
