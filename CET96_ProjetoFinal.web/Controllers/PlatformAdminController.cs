using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformAdminController"/> class.
        /// </summary>
        /// <param name="userRepository">The repository for user data operations.</param>
        /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
        public PlatformAdminController(IApplicationUserRepository userRepository, UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
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
                    Roles = roles
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

            // If the check passes, show the confirmation view.
            return View(userToDeactivate);
        }

        /// <summary>
        /// Handles the POST request to confirm and perform the deactivation of a user account.
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

            // FINAL SECURITY CHECK: Double-check the role before performing the action.
            if (await _userManager.IsInRoleAsync(userToDeactivate, "Platform Administrator"))
            {
                TempData["StatusMessage"] = "Error: Platform Administrator accounts cannot be deactivated.";
                return RedirectToAction("UserManager");
            }

            // Deactivate the target user
            var adminWhoIsDeactivating = await _userManager.GetUserAsync(User);
            userToDeactivate.DeactivatedAt = DateTime.UtcNow;
            userToDeactivate.DeactivatedByUserId = adminWhoIsDeactivating.Id;
            await _userRepository.UpdateUserAsync(userToDeactivate);
            await _userManager.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

            TempData["StatusMessage"] = $"User {userToDeactivate.Email} has been successfully deactivated.";
            return RedirectToAction("UserManager");
        }

        /// <summary>
        /// Activates a previously deactivated user account.
        /// </summary>
        /// <param name="id">The ID of the user to activate.</param>
        /// <returns>A redirect to the user management page.</returns>
        public async Task<IActionResult> ActivateUser(string id)
        {
            var platformAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var userToActivate = await _userRepository.GetUserByIdAsync(id);

            if (userToActivate == null) return NotFound();

            // 1. Clear the lockout end date to unlock the account
            await _userRepository.SetLockoutEndDateAsync(userToActivate, null);

            // 2. Clear any deactivation audit fields
            userToActivate.DeactivatedAt = null;
            userToActivate.DeactivatedByUserId = null; // Also clear who deactivated them
            await _userRepository.UpdateUserAsync(userToActivate);

            TempData["StatusMessage"] = $"User {userToActivate.Email} has been activated.";

            return RedirectToAction("UserManager");
        }

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