using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    /// <summary>
    /// Manages platform-wide administrative actions, such as user management.
    /// Restricted to users in the 'Platform Administrator' role.
    /// </summary>
    [Authorize(Roles = "Platform Administrator")]
    public class PlatformAdminController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICompanyRepository _companyRepository;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly ApplicationUserDataContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PlatformAdminController> _logger; // Recommended for logging errors

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformAdminController"/> class.
        /// </summary>
        /// <param name="userRepository">The repository for user data operations.</param>
        /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
        public PlatformAdminController(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            ICompanyRepository companyRepository,
            ICondominiumRepository condominiumRepository,
            ApplicationUserDataContext context,
            IEmailSender emailSender,
            ILogger<PlatformAdminController> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _companyRepository = companyRepository;
            _condominiumRepository = condominiumRepository;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Displays the main user management dashboard with a list of all system users.
        /// </summary>
        /// <returns>The user management view populated with all users.</returns>
        public async Task<IActionResult> UserManager()
        {
            var allUsers = await _userRepository.GetAllUsersAsync();
            var userViewModelList = new List<ApplicationUserViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userRepository.GetUserRolesAsync(user);
                userViewModelList.Add(new ApplicationUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    IsDeactivated = user.DeactivatedAt.HasValue,
                    Roles = roles,
                    IsEmailConfirmed = user.EmailConfirmed
                });
            }

            // This sorts the list created according to your custom role order ( GetRoleSortOrder() ).
            var sortedUsers = userViewModelList.OrderBy(u => u.Roles.Select(GetRoleSortOrder).Min());

            var model = new HomeViewModel
            {
                AllUsers = sortedUsers
            };

            return View(model); // This will return the new Views/PlatformAdmin/UserManager.cshtml
        }

        /// <summary>
        /// Displays the confirmation page before deactivating a user's account.
        /// If the target user is a Company Administrator, this view will include a
        /// critical warning about the cascading deactivation of their entire company.
        /// </summary>
        /// <param name="id">The ID of the user to deactivate.</param>
        /// <returns>The confirmation view, or a redirect if the action is not permitted.</returns>
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
            if (userToDeactivate == null)
            {
                return NotFound();
            }

            // FINAL SECURITY CHECK: Is the target a Platform Administrator?
            if (await _userManager.IsInRoleAsync(userToDeactivate, "Platform Administrator"))
            {
                TempData["StatusMessage"] = "Error: Platform Administrator accounts cannot be deactivated.";
                return RedirectToAction("UserManager");
            }

            // --- Check if this is a Company Admin to show a warning ---
            if (await _userManager.IsInRoleAsync(userToDeactivate, "Company Administrator") && userToDeactivate.CompanyId.HasValue)
            {
                var company = await _companyRepository.GetByIdAsync(userToDeactivate.CompanyId.Value);
                ViewBag.WarningMessage = $"This user is the administrator for {company?.Name}. Deactivating this account will ALSO deactivate the company, ALL of its condominiums, and ALL associated staff and manager accounts.";
            }

            // If the check passes, show the confirmation view.
            return View(userToDeactivate);
        }

        /// <summary>
        /// Handles the POST request to confirm and lock a user account.
        /// CRITICAL: If the user is a 'Company Administrator', this triggers a
        /// cascade-lock of ALL associated user accounts (Managers and Staff) for ALL companies they manage.
        /// This action sends notifications to the company's official email, all affected users, and the Platform Admin.
        /// </summary>
        /// <param name="id">The ID of the user to be locked.</param>
        /// <returns>A redirect to the user management page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUserConfirm(string id)
        {
            var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
            if (userToDeactivate == null) return NotFound();

            var platformAdmin = await _userManager.GetUserAsync(User);

            if (userToDeactivate.Id == platformAdmin.Id || await _userManager.IsInRoleAsync(userToDeactivate, "Platform Administrator"))
            {
                TempData["StatusMessage"] = "Error: Platform Administrator accounts cannot be deactivated.";
                return RedirectToAction("UserManager");
            }

            bool isCompanyAdmin = await _userManager.IsInRoleAsync(userToDeactivate, "Company Administrator");

            if (isCompanyAdmin)
            {
                _logger.LogInformation($"Starting cascade-lock for CompanyAdmin {userToDeactivate.Email}...");

                // 1. Find ALL companies for this admin using the new method
                var companiesToLock = await _companyRepository.GetAllCompaniesByAdminIdAsync(userToDeactivate.Id);

                if (companiesToLock != null && companiesToLock.Any())
                {
                    var usersToLock = new List<ApplicationUser> { userToDeactivate }; // Add the admin
                    var companyNames = new List<string>();

                    // 2. Loop through each company to find all subordinate users
                    foreach (var company in companiesToLock)
                    {
                        companyNames.Add(company.Name);

                        // 2a. Get managers
                        var managers = await _userRepository.GetUsersByCompanyIdAsync(company.Id);
                        usersToLock.AddRange(managers);

                        // 2b. Get staff
                        var condominiums = await _condominiumRepository.GetCondominiumsByCompanyIdAsync(company.Id);
                        foreach (var condo in condominiums)
                        {
                            var staff = await _userRepository.GetStaffByCondominiumIdAsync(condo.Id);
                            usersToLock.AddRange(staff);
                        }
                    }

                    var affectedUsers = usersToLock.DistinctBy(u => u.Id).ToList();

                    // 3. Lock all users
                    foreach (var user in affectedUsers)
                    {
                        user.DeactivatedAt = DateTime.UtcNow;
                        user.DeactivatedByUserId = platformAdmin.Id;
                        await _userManager.UpdateAsync(user);
                        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    }

                    // 4. Send Notifications
                    foreach (var company in companiesToLock)
                    {
                        if (!string.IsNullOrEmpty(company.Email))
                        {
                            await _emailSender.SendEmailAsync(company.Email,
                                $"Official Notification: Company Accounts Locked - {company.Name}",
                                $"<p>This is an official notification that all user accounts associated with your company, '{company.Name}', have been locked by a Platform Administrator.</p>");
                        }
                    }

                    // Send to all affected users
                    var userEmailBody = "<p>Your CondoManagerPrime account has been locked by a platform administrator as part of a company-wide deactivation.</p>" +
                                        "<p>If you believe this is in error, please contact your company administrator or system support.</p>";
                    foreach (var user in affectedUsers)
                    {
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            try
                            {
                                await _emailSender.SendEmailAsync(user.Email, "Your Account Has Been Locked", userEmailBody);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to send deactivation email to {user.Email}");
                            }
                        }
                    }

                    // Send summary to Platform Admin
                    var userListHtml = $"<ul>{string.Join("", affectedUsers.Select(u => $"<li>{u.Email} ({u.FirstName} {u.LastName})</li>"))}</ul>";
                    var adminEmailBody = $"<p>You have successfully locked {affectedUsers.Count} user accounts associated with the Company Administrator {userToDeactivate.Email}.</p>" +
                                         $"<p>The following companies were affected:</p>" +
                                         $"<ul>{string.Join("", companyNames.Select(n => $"<li>{n}</li>"))}</ul>" +
                                         $"<p>The following user accounts were locked:</p>" +
                                         $"{userListHtml}";

                    await _emailSender.SendEmailAsync(platformAdmin.Email, $"Lock Report: {userToDeactivate.Email}", adminEmailBody);
                    await _emailSender.SendEmailAsync("prme-condo-test@yopmail.com", "YOPMAIL TEST - Lock Report", adminEmailBody);

                    TempData["StatusMessage"] = $"Administrator {userToDeactivate.Email} and all {affectedUsers.Count} associated user accounts have been locked.";
                    return RedirectToAction("UserManager");
                }
            }

            // --- Simple user deactivation (not a Company Admin, or a Company Admin with 0 companies) ---
            userToDeactivate.DeactivatedAt = DateTime.UtcNow;
            userToDeactivate.DeactivatedByUserId = platformAdmin.Id;
            await _userManager.UpdateAsync(userToDeactivate);
            await _userManager.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);
            await _emailSender.SendEmailAsync(userToDeactivate.Email, "Your Account Has Been Locked", "<p>Your CondoManagerPrime account has been manually locked by a platform administrator.</p>");

            TempData["StatusMessage"] = $"User {userToDeactivate.Email} has been successfully locked.";
            return RedirectToAction("UserManager");
        }

        /// <summary>
        /// Activates a previously locked user account.
        /// CRITICAL: If the user is a 'Company Administrator', this triggers a
        /// cascade-unlock of ALL associated user accounts (Managers and Staff).
        /// </summary>
        /// <param name="id">The ID of the user to activate.</param>
        /// <returns>A redirect to the user management page.</returns>
        public async Task<IActionResult> ActivateUser(string id)
        {
            var userToActivate = await _userRepository.GetUserByIdAsync(id);
            if (userToActivate == null) return NotFound();

            var platformAdmin = await _userManager.GetUserAsync(User);

            bool isCompanyAdmin = await _userManager.IsInRoleAsync(userToActivate, "Company Administrator");
            var affectedUsers = new List<ApplicationUser> { userToActivate }; // Add the primary user to the list

            List<Company> companies = null;

            if (isCompanyAdmin)
            {
                _logger.LogInformation($"Starting cascade-unlock for CompanyAdmin {userToActivate.Email}...");

                // 1. Find ALL companies for this admin
                companies = (await _companyRepository.GetAllCompaniesByAdminIdAsync(userToActivate.Id)).ToList();

                if (companies != null && companies.Any())
                {
                    var usersToUnlock = new List<ApplicationUser>();

                    // 2. Loop through each company to find all subordinate users
                    foreach (var company in companies)
                    {
                        // 2a. Get managers
                        var managers = await _userRepository.GetUsersByCompanyIdAsync(company.Id);
                        usersToUnlock.AddRange(managers);

                        // 2b. Get staff
                        var condominiums = await _condominiumRepository.GetCondominiumsByCompanyIdAsync(company.Id);
                        foreach (var condo in condominiums)
                        {
                            var staff = await _userRepository.GetStaffByCondominiumIdAsync(condo.Id);
                            usersToUnlock.AddRange(staff);
                        }
                    }
                    affectedUsers.AddRange(usersToUnlock.DistinctBy(u => u.Id));
                    affectedUsers = affectedUsers.DistinctBy(u => u.Id).ToList();
                }
            }

            // 3. Unlock all users in the list
            foreach (var user in affectedUsers)
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // Unlock
                user.DeactivatedAt = null;
                user.DeactivatedByUserId = null;
                await _userManager.UpdateAsync(user); // Save audit fields
            }

            // 4. Send Notifications
            if (isCompanyAdmin && companies != null)
            {
                foreach (var company in companies)
                {
                    if (!string.IsNullOrEmpty(company.Email))
                    {
                        await _emailSender.SendEmailAsync(
                            company.Email,
                            $"Official Notification: Company Accounts Reactivated - {company.Name}",
                            $"<p>This is an official notification that your company, '{company.Name}', and its associated user accounts have been reactivated by a Platform Administrator.</p>"
                        );
                    }
                }
            }

            // Send to all affected users
            var userEmailBody = "<p>Your CondoManagerPrime account, and any associated staff accounts, have been reactivated by the Platform Administrator. You can now log in.</p>";
            foreach (var user in affectedUsers)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Your Account Has Been Reactivated", userEmailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send activation email to {user.Email}");
                    }
                }
            }

            // Send summary to Platform Admin
            var userListHtml = $"<ul>{string.Join("", affectedUsers.Select(u => $"<li>{u.Email} ({u.FirstName} {u.LastName})</li>"))}</ul>";
            var adminEmailBody = $"<p>You have successfully activated {affectedUsers.Count} user accounts associated with {userToActivate.Email}.</p>" +
                                 $"<p>The following user accounts were activated:</p>" +
                                 $"{userListHtml}";
            await _emailSender.SendEmailAsync(platformAdmin.Email, $"Activation Report: {userToActivate.Email}", adminEmailBody);
            await _emailSender.SendEmailAsync("prme-condo-test@yopmail.com", "YOPMAIL TEST - Activation Report", adminEmailBody);

            TempData["StatusMessage"] = $"Successfully activated {affectedUsers.Count} user account(s).";
            return RedirectToAction("UserManager");
        }

        /// <summary>
        /// Determines the sort order for a given role name.
        /// </summary>
        /// <param name="roleName">The name of the role for which to determine the sort order.</param>
        /// <returns>An integer representing the sort order of the specified role. Lower values indicate higher priority. Returns
        /// 99 if the role name does not match any predefined roles.</returns>
        private int GetRoleSortOrder(string roleName)
        {
            switch (roleName)
            {
                case "Platform Administrator": return 1;
                case "Company Administrator": return 2;
                case "Condominium Manager": return 3;
                case "Condominium Staff": return 4;
                case "Unit Owner": return 5;
                default: return 99; // Other roles go to the bottom
            }
        }
    }
}