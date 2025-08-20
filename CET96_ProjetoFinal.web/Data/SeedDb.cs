using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Data
{
    public class SeedDb
    {
        private readonly ApplicationUserDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SeedDb(ApplicationUserDataContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

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