using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Data
{
    /// <summary>
    /// Provides functionality to seed the database with initial data, including roles and a platform administrator
    /// user.
    /// </summary>
    /// <remarks>This class is responsible for ensuring that the database is properly initialized with
    /// required roles and a default platform administrator account. It is typically used during application startup to
    /// prepare the database for use.</remarks>
    public class SeedDb
    {
        private readonly ApplicationUserDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;

        public SeedDb(ApplicationUserDataContext context,
                  UserManager<ApplicationUser> userManager,
                  RoleManager<IdentityRole> roleManager,
                  IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager; // Manages user creation, deletion, and role assignments
            _roleManager = roleManager; // Manages roles within the application
            _emailSender = emailSender;
        }

        /// <summary>
        /// Seeds the database with initial data, including roles and a platform administrator user.
        /// </summary>
        /// <remarks>This method ensures that the database schema is up-to-date by applying any pending
        /// migrations. It then creates predefined application roles if they do not already exist and adds a platform
        /// administrator user with the highest level of access. The platform administrator user is created with a
        /// default password and assigned the "Platform Administrator" role.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SeedAsync()
        {
            // Ensures the database and tables are created based on migrations
            await _context.Database.MigrateAsync();

            // --- 1. Create Application Roles ---
            string[] roleNames = { "Platform Administrator", "Company Administrator", "Condominium Manager", "Unit Owner", "Condominium Staff" };

            foreach (var roleName in roleNames)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- 2. Create the Platform Administrator User ---
            // This user has the highest level of access and manages the entire platform.
            // It should not be creatable from any public-facing registration page.

            var platformAdminUser = await _userManager.FindByEmailAsync("nuno.goncalo.gomes@formandos.cinel.pt");
            if (platformAdminUser == null)
            {
                platformAdminUser = new ApplicationUser
                {
                    UserName = "nuno.goncalo.gomes@formandos.cinel.pt",
                    Email = "nuno.goncalo.gomes@formandos.cinel.pt",
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    IdentificationDocument = "000000000",
                    DocumentType = DocumentTypeEnum.Other,
                    PhoneNumber = "000000000",
                    CompanyName = "Platform Administration"
                };

                // This try/catch block will catch any type of error during user creation
                try
                {
                    var result = await _userManager.CreateAsync(platformAdminUser, "123456");

                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(platformAdminUser, "Platform Administrator");

                        // Optionally, send an email to the platform admin with their credentials.
                        //string subject = "Your CondoManagerPrime Admin Account";
                        //string message = $"Hello, your admin account has been created.<br/>" +
                        //                 $"Username: {platformAdminUser.Email}<br/>" +
                        //                 $"Password: 123456";

                        //await _emailSender.SendEmailAsync(
                        //    platformAdminUser.Email,
                        //    subject,
                        //    message);
                    }
                    else
                    {
                        // This 'else' block catches graceful failures, like password policy issues.
                        // You can inspect the 'result.Errors' here to see the problem.
                        System.Diagnostics.Debugger.Break();
                    }
                }
                catch (Exception ex)
                {
                    // This 'catch' block will catch hard crashes like the SqlNullValueException.
                    // You can inspect the 'ex' object here to see the real exception details.
                    System.Diagnostics.Debugger.Break();
                }
            }
        }
    }
}