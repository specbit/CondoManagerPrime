using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CET96_ProjetoFinal.web.Data
{
    public class MockSeedDb
    {
        private readonly ApplicationUserDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MockSeedDb(ApplicationUserDataContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await _context.Database.MigrateAsync();

            // Create Application Roles
            string[] roleNames = { "Platform Administrator", "Company Administrator", "Condominium Manager", "Unit Owner", "Condominium Staff" };
            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create the Platform Administrator User
            var platformAdminUser = await _userManager.FindByEmailAsync("nuno.goncalo.gomes@formandos.cinel.pt");
            if (platformAdminUser == null)
            {
                platformAdminUser = new ApplicationUser { /* ... platform admin details ... */ };
                var result = await _userManager.CreateAsync(platformAdminUser, "123456");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(platformAdminUser, "Platform Administrator");
                }
            }

            // Create the 'deleteme@yopmail.com' Test User and Company
            var deleteMeUser = await _userManager.FindByEmailAsync("deleteme@yopmail.com");
            if (deleteMeUser == null)
            {
                deleteMeUser = new ApplicationUser
                {
                    FirstName = "Delete",
                    LastName = "Me",
                    UserName = "deleteme@yopmail.com",
                    Email = "deleteme@yopmail.com",
                    EmailConfirmed = true,
                    IdentificationDocument = "987654321",
                    DocumentType = DocumentTypeEnum.CitizenCard,
                    PhoneNumber = "987654321",
                    CompanyName = "DeleteMe Corp"
                };
                var userResult = await _userManager.CreateAsync(deleteMeUser, "123456");
                if (userResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(deleteMeUser, "Company Administrator");
                    if (!await _context.Companies.AnyAsync(c => c.Name == "DeleteMe Corp"))
                    {
                        var company = new Company
                        {
                            Name = "DeleteMe Corp",
                            Description = "A test company seeded from the database.",
                            TaxId = "123456789",
                            Address = "321 Delete Avenue, Porto",
                            PhoneNumber = "229876543",
                            Email = "deletecompany@yopmail.com",
                            ApplicationUserId = deleteMeUser.Id,
                            UserCreatedId = deleteMeUser.Id,
                            PaymentValidated = true
                        };
                        _context.Companies.Add(company);
                        await _context.SaveChangesAsync();
                        deleteMeUser.CompanyId = company.Id;
                        await _userManager.UpdateAsync(deleteMeUser);
                    }
                }
            }
        }
    }
}
