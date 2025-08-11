using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // This gets the connection string from your appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("ApplicationUserConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // This registers the DbContext with the application's services
            builder.Services.AddDbContext<ApplicationUserDataContext>(options =>
                options.UseSqlServer(connectionString));

            // Configure Identity services
            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true; // Ensure unique email addresses
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false; // relax password policy for testing
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
            })
                .AddRoles<IdentityRole>() // Using roles
                .AddEntityFrameworkStores<ApplicationUserDataContext>();

            // Register custom application services

            // Register the custom helper for user operations
            builder.Services.AddScoped<IApplicationUserHelper, ApplicationUserHelper>();

            // Register SeedDb service to seed the database
            builder.Services.AddTransient<SeedDb>(); // Register the Seeder

            var app = builder.Build();

            // Call the RunSeeding method to seed the database with initial data
            RunSeeding(app);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static void RunSeeding(IHost host)
        {
            var scopeFactory = host.Services.GetService<IServiceScopeFactory>();
            using (var scope = scopeFactory?.CreateScope())
            {
                var seeder = scope?.ServiceProvider.GetService<SeedDb>();
                if (seeder != null)
                {
                    seeder.SeedAsync().Wait();
                }
                else
                {
                    var logger = scope?.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError("Failed to retrieve SeedDB service during startup.");
                }
            }
        }

    }
}
