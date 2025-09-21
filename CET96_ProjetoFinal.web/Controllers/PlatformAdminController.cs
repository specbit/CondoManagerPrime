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

        // TODO: Delete this old code if the new cascade-deactivate code is approved.
        ///// <summary>
        ///// Displays the confirmation page before deactivating a user's account.
        ///// </summary>
        ///// <param name="id">The ID of the user to deactivate.</param>
        ///// <returns>The confirmation view, or a redirect if the action is not permitted.</returns>
        //public async Task<IActionResult> DeactivateUser(string id)
        //{
        //    var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
        //    if (userToDeactivate == null)
        //    {
        //        return NotFound();
        //    }

        //    // FINAL SECURITY CHECK: Is the target a Platform Administrator?
        //    if (await _userManager.IsInRoleAsync(userToDeactivate, "Platform Administrator"))
        //    {
        //        TempData["StatusMessage"] = "Error: Platform Administrator accounts cannot be deactivated.";
        //        return RedirectToAction("UserManager");
        //    }

        //    // If the check passes, show the confirmation view.
        //    return View(userToDeactivate);
        //}

        ///// <summary>
        ///// Handles the POST request to confirm and perform the deactivation of a user account.
        ///// </summary>
        ///// <param name="id">The ID of the user to be deactivated.</param>
        ///// <returns>A redirect to the user management page.</returns>
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeactivateUserConfirm(string id)
        //{
        //    var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
        //    if (userToDeactivate == null)
        //    {
        //        return NotFound();
        //    }

        //    // FINAL SECURITY CHECK: Double-check the role before performing the action.
        //    if (await _userManager.IsInRoleAsync(userToDeactivate, "Platform Administrator"))
        //    {
        //        TempData["StatusMessage"] = "Error: Platform Administrator accounts cannot be deactivated.";
        //        return RedirectToAction("UserManager");
        //    }

        //    // Deactivate the target user
        //    var adminWhoIsDeactivating = await _userManager.GetUserAsync(User);
        //    userToDeactivate.DeactivatedAt = DateTime.UtcNow;
        //    userToDeactivate.DeactivatedByUserId = adminWhoIsDeactivating.Id;
        //    await _userRepository.UpdateUserAsync(userToDeactivate);
        //    await _userManager.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

        //    TempData["StatusMessage"] = $"User {userToDeactivate.Email} has been successfully deactivated.";
        //    return RedirectToAction("UserManager");
        //}

        ///// <summary>
        ///// Activates a previously deactivated user account.
        ///// </summary>
        ///// <param name="id">The ID of the user to activate.</param>
        ///// <returns>A redirect to the user management page.</returns>
        //public async Task<IActionResult> ActivateUser(string id)
        //{
        //    var platformAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
        //    var userToActivate = await _userRepository.GetUserByIdAsync(id);

        //    if (userToActivate == null) return NotFound();

        //    // 1. Clear the lockout end date to unlock the account
        //    await _userRepository.SetLockoutEndDateAsync(userToActivate, null);

        //    // 2. Clear any deactivation audit fields
        //    userToActivate.DeactivatedAt = null;
        //    userToActivate.DeactivatedByUserId = null; // Also clear who deactivated them
        //    await _userRepository.UpdateUserAsync(userToActivate);

        //    TempData["StatusMessage"] = $"User {userToActivate.Email} has been activated.";

        //    return RedirectToAction("UserManager");
        //}

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
        /// Handles the POST request to confirm and perform the deactivation of a user account.
        /// CRITICAL: If the target user is in the 'Company Administrator' role, this will trigger a database
        /// transaction to perform a cascade deactivation, shutting down the entire tenant. This includes:
        /// 1. Deactivating the Company entity.
        /// 2. Deactivating all associated Condominium entities.
        /// 3. Deactivating and Locking ALL user accounts associated with that company OR any of its condominiums
        ///    (including other admins, managers, and staff).
        /// 4. Sends a summary email to the Platform Admin and notification emails to all deactivated users.
        /// If the user is not a Company Admin, it will only deactivate the single user account.
        /// </summary>
        /// <param name="id">The ID of the user to be deactivated.</param>
        /// <returns>A redirect to the user management page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUserConfirm(string id)
        {
            var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
            if (userToDeactivate == null)
            {
                return NotFound();
            }

            // Security Check: Cannot deactivate a Platform Admin
            if (await _userManager.IsInRoleAsync(userToDeactivate, "Platform Administrator"))
            {
                TempData["StatusMessage"] = "Error: Platform Administrator accounts cannot be deactivated.";
                return RedirectToAction("UserManager");
            }

            var platformAdmin = await _userManager.GetUserAsync(User);
            bool isCompanyAdmin = await _userManager.IsInRoleAsync(userToDeactivate, "Company Administrator");

            if (isCompanyAdmin && userToDeactivate.CompanyId.HasValue)
            {
                // --- CASCADE DEACTIVATION WORKFLOW ---
                int companyId = userToDeactivate.CompanyId.Value;
                List<ApplicationUser> allUniqueUsers = new List<ApplicationUser>();
                List<string> condominiumNames = new List<string>();
                string companyName = "N/A";
                string companyEmail = null;

                await using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Deactivate Company
                        var company = await _companyRepository.GetByIdAsync(companyId);
                        if (company != null)
                        {
                            companyName = company.Name;
                            companyEmail = company.Email;
                            company.IsActive = false;
                            company.DeletedAt = DateTime.UtcNow;
                            _companyRepository.Update(company);
                        }

                        // 2. Deactivate Condominiums
                        var condominiums = await _condominiumRepository.GetActiveCondominiumsByCompanyIdAsync(companyId); // Get active ones to deactivate
                        var condominiumIds = new List<int>();
                        foreach (var condo in condominiums)
                        {
                            condo.IsActive = false;
                            condo.DeletedAt = DateTime.UtcNow;
                            _condominiumRepository.Update(condo);
                            condominiumIds.Add(condo.Id);
                            condominiumNames.Add(condo.Name); // Store name for email
                        }

                        // 3. Get all Users to deactivate
                        var usersToDeactivate = new List<ApplicationUser>();

                        // Add all users linked by CompanyId (all Company Admins, all Condo Managers)
                        var companyUsers = await _userRepository.GetUsersByCompanyIdAsync(companyId);
                        usersToDeactivate.AddRange(companyUsers);

                        // Add all staff users linked to each condominium
                        foreach (var condoId in condominiumIds)
                        {
                            var staffUsers = await _userRepository.GetStaffByCondominiumIdAsync(condoId);
                            usersToDeactivate.AddRange(staffUsers);
                        }

                        // Final unique list
                        allUniqueUsers = usersToDeactivate.DistinctBy(u => u.Id).ToList();

                        // 4. Deactivate and Lock every user in the transaction
                        foreach (var user in allUniqueUsers)
                        {
                            user.DeactivatedAt = DateTime.UtcNow;
                            user.DeactivatedByUserId = platformAdmin.Id;
                            await _userManager.UpdateAsync(user); // Use UserManager to update user
                            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                        }

                        // 5. If everything succeeded, commit the transaction
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error during cascade-deactivate transaction.");
                        TempData["StatusMessage"] = "Error: A critical failure occurred during the cascade-deactivation. No changes were made.";
                        return RedirectToAction("UserManager");
                    }
                }

                // --- SEND EMAILS (Only after transaction is committed) ---

                // 1. Send Summary Email to Platform Admin
                var condoListHtml = condominiumNames.Any() ? $"<ul>{string.Join("", condominiumNames.Select(n => $"<li>{n}</li>"))}</ul>" : "<p>No active condominiums were deactivated.</p>";
                var userListHtml = $"<p>{allUniqueUsers.Count} total user account(s) have been deactivated and locked.</p>";
                var adminEmailBody = $"<h3>Deactivation Summary</h3>" +
                                     $"<p>You have successfully deactivated the following administrator and all their associated assets:</p>" +
                                     $"<ul>" +
                                     $"<li><b>Administrator:</b> {userToDeactivate.FirstName} {userToDeactivate.LastName} ({userToDeactivate.Email})</li>" +
                                     $"<li><b>Company:</b> {companyName}</li>" +
                                     $"</ul>" +
                                     $"<b>Deactivated Condominiums:</b>" +
                                     $"{condoListHtml}" +
                                     $"{userListHtml}";

                await _emailSender.SendEmailAsync(platformAdmin.Email, $"Deactivation Report: {companyName}", adminEmailBody);

                // 2. Send Notification Email to ALL Deactivated Users
                var userEmailBody = "<p>Your CondoManagerPrime account has been deactivated by a platform administrator as part of a company-wide action.</p>" +
                                    "<p>If you believe this is in error, please contact your company administrator or system support.</p>";

                // 3. Send totification to the company' official email
                if (!string.IsNullOrEmpty(companyEmail))
                {
                    await _emailSender.SendEmailAsync(
                        companyEmail,
                        $"Official Notification: Company Deactivated - {companyName}",
                        $"<p>This is an official notification that your company, '{companyName}', and all associated user accounts and condominiums have been deactivated by a Platform Administrator.</p>" +
                        "<p>Please contact support for further information.</p>"
                    );
                }

                foreach (var user in allUniqueUsers)
                {
                    // Avoid sending an email if the email address is somehow null or empty
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Your Account Has Been Deactivated", userEmailBody);
                    }
                }

                TempData["StatusMessage"] = $"Company '{companyName}', all its condominiums, and {allUniqueUsers.Count} user accounts have been successfully deactivated. Email notifications have been sent.";
            }
            else
            {
                // --- This user is not a Company Admin, so just deactivate them.
                userToDeactivate.DeactivatedAt = DateTime.UtcNow;
                userToDeactivate.DeactivatedByUserId = platformAdmin.Id;
                await _userRepository.UpdateUserAsync(userToDeactivate);
                await _userManager.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

                await _emailSender.SendEmailAsync(userToDeactivate.Email, "Your Account Has Been Deactivated", "<p>Your CondoManagerPrime account has been manually deactivated by a platform administrator.</p>");

                TempData["StatusMessage"] = $"User {userToDeactivate.Email} has been successfully deactivated.";
            }

            return RedirectToAction("UserManager");
        }

        /// <summary>
        /// Activates a previously deactivated user account.
        /// If the user is a Company Administrator, this will also cascade-up and
        /// reactivate their parent Company and all associated Condominium entities.
        /// It does NOT reactivate other subordinate user accounts, which must be
        /// done manually by the reactivated administrator.
        /// </summary>
        /// <param name="id">The ID of the user to activate.</param>
        /// <returns>A redirect to the user management page.</returns>
        public async Task<IActionResult> ActivateUser(string id)
        {
            var userToActivate = await _userRepository.GetUserByIdAsync(id);
            if (userToActivate == null) return NotFound();

            // 1. GET THE PLATFORM ADMIN (for the email) 
            var platformAdmin = await _userManager.GetUserAsync(User);

            var successMessage = $"User {userToActivate.Email} has been activated.";

            // 2. Clear the lockout end date to unlock the account
            await _userManager.SetLockoutEndDateAsync(userToActivate, null);

            // 3. Clear any deactivation audit fields
            userToActivate.DeactivatedAt = null;
            userToActivate.DeactivatedByUserId = null; // Also clear who deactivated them

            // Note: UpdateUserAsync might not be needed if SaveChangesAsync is called later,
            // but it's good practice with the repository pattern.
            await _userRepository.UpdateUserAsync(userToActivate);

            // --- START: NEW CASCADE-UP LOGIC ---
            bool isCompanyAdmin = await _userManager.IsInRoleAsync(userToActivate, "Company Administrator");

            if (isCompanyAdmin && userToActivate.CompanyId.HasValue)
            {
                var companyId = userToActivate.CompanyId.Value;
                string companyName = "N/A";
                string companyEmail = null;

                // 4. Reactivate the Company
                var company = await _companyRepository.GetByIdAsync(companyId);
                if (company != null)
                {
                    companyName = company.Name;
                    companyEmail = company.Email;
                    company.IsActive = true;
                    company.DeletedAt = null;
                    _companyRepository.Update(company);
                }

                // 5. Reactivate all Condominiums in that Company
                var condominiums = await _condominiumRepository.GetActiveCondominiumsByCompanyIdAsync(companyId);
                foreach (var condo in condominiums)
                {
                    condo.IsActive = true;
                    condo.DeletedAt = null;
                    _condominiumRepository.Update(condo);
                }

                // 6. Save all entity changes to the database
                // This single SaveChanges call commits all updates from all repositories
                await _context.SaveChangesAsync();

                successMessage = $"User {userToActivate.Email}, Company '{companyName}', and all associated condominiums have been reactivated.";

                // 7. Send Email to Company Admin
                // They must now manually reactivate their own staff.
                await _emailSender.SendEmailAsync(userToActivate.Email,
                    "Your Account and Company Have Been Reactivated",
                    $"<p>Your account, your company ({companyName}), and all its condominiums have been reactivated by the Platform Administrator.</p>" +
                    "<p>Please note: All subordinate user accounts (such as managers and staff) remain inactive. You must log in and manually reactivate any accounts you wish to restore.</p>");

                // 8. Send Notification to the Company's Official Email 
                if (!string.IsNullOrEmpty(companyEmail))
                {
                    await _emailSender.SendEmailAsync(
                        companyEmail,
                        $"Official Notification: Company Reactivated - {companyName}",
                        $"<p>This is an official notification that your company, '{companyName}', has been reactivated by a Platform Administrator.</p>" +
                        $"<p>The administrator {userToActivate.FirstName} {userToActivate.LastName} ({userToActivate.Email}) has also been reactivated.</p>"
                    );
                }

                // 9. Send Summary Email to Platform Admin ---
                if (platformAdmin != null)
                {
                    await _emailSender.SendEmailAsync(
                        platformAdmin.Email,
                        $"Activation Report: {companyName}",
                        $"<p>This is a confirmation that you have successfully reactivated the company <strong>{companyName}</strong> and its administrator {userToActivate.FirstName} {userToActivate.LastName} ({userToActivate.Email}).</p>"
                    );
                }
            }
            // --- END: NEW CASCADE-UP LOGIC ---

            TempData["StatusMessage"] = successMessage;

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