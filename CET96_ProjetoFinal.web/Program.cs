using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
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

            // --- User Database Context ---
            // This gets the connection string from appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("ApplicationUserConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // This registers the DbContext with the application's services
            builder.Services.AddDbContext<ApplicationUserDataContext>(options =>
                options.UseSqlServer(connectionString));

            // --- Condominium Database Context  ---
            // This gets the new connection string from appsettings.json
            var condominiumConnectionString = builder.Configuration.GetConnectionString("CondominiumConnection")
                ?? throw new InvalidOperationException("Connection string 'CondominiumConnection' not found.");

            // This registers the new DbContext using the new connection string
            builder.Services.AddDbContext<CondominiumDataContext>(options =>
                options.UseSqlServer(condominiumConnectionString));

            // Configure Identity services

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // This requires that a user's email must be confirmed before they can sign in.
                options.User.RequireUniqueEmail = true; // Ensure unique email addresses
                options.SignIn.RequireConfirmedAccount = true;

                // Your relaxed password policy
                options.Password.RequireDigit = false; // relax password policy for testing
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
            })
                .AddRoles<IdentityRole>() // Using roles
                .AddEntityFrameworkStores<ApplicationUserDataContext>()
                .AddDefaultTokenProviders(); // Important for password reset and email confirmation tokens

            // This adds the cookie authentication handler
            // Explicitly configures the cookie
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            {
                // This forces the application to check the user's security stamp in the database
                // on every request. If the user is deleted, the check will fail, and they will be logged out.
                options.ValidationInterval = TimeSpan.Zero;
            });


            // Register the custom email sender service
            //builder.Services.AddTransient<IEmailSender, DebugEmailSender>();
            builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
            //builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

            // Register custom application services
            //builder.Services.AddScoped<IApplicationUserHelper, ApplicationUserHelper>();
            builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
            builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
            builder.Services.AddScoped<ICondominiumRepository, CondominiumRepository>();
            builder.Services.AddScoped<IUnitRepository, UnitRepository>();

            // Register SeedDb service to seed the database
            builder.Services.AddTransient<SeedDb>(); // Register the Seeder
            //builder.Services.AddTransient<MockSeedDb>(); // Register the Seeder

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
