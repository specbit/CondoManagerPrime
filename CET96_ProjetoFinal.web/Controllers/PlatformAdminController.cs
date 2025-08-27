using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Platform Administrator")]
    public class PlatformAdminController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public PlatformAdminController(IApplicationUserRepository userRepository, UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
        }

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

            var model = new HomeViewModel
            {
                AllUsers = userViewModelList
            };

            return View(model); // This will return the new Views/PlatformAdmin/UserManager.cshtml
        }

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
        /// Handles the deactivation of a Company Administrator and all their associated active companies.
        /// </summary>
        /// <param name="userIdToDeactivate">The ID of the user being deactivated.</param>
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeactivateUserConfirm(string userIdToDeactivate)
        //{
        //    var platformAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);

        //    // CHECK: Add the same check here as a final security measure
        //    if (platformAdmin.Id == userIdToDeactivate)
        //    {
        //        TempData["StatusMessage"] = "Error: You cannot deactivate your own account.";
        //        return RedirectToAction("UserManager");
        //    }

        //    var userToDeactivate = await _userRepository.GetUserByIdAsync(userIdToDeactivate);

        //    if (userToDeactivate == null) return NotFound();

        //    //// 1. Deactivate all companies owned by this user
        //    //var companies = await _companyRepository.GetCompaniesByUserIdAsync(userToDeactivate.Id);
        //    //foreach (var company in companies)
        //    //{
        //    //    company.IsActive = false;
        //    //    company.DeletedAt = DateTime.UtcNow;
        //    //    company.UserDeletedId = platformAdmin.Id;
        //    //    _companyRepository.Update(company);
        //    //}

        //    // 2. Lock the user's account permanently and update audit fields
        //    userToDeactivate.DeactivatedAt = DateTime.UtcNow;
        //    userToDeactivate.DeactivatedByUserId = platformAdmin.Id;
        //    await _userRepository.UpdateUserAsync(userToDeactivate);
        //    await _userRepository.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

        //    //// 3. Save all changes to the company database
        //    //await _companyRepository.SaveAllAsync();

        //    TempData["StatusMessage"] = $"User {userToDeactivate.Email} has been deactivated.";

        //    return RedirectToAction("HomePlatformAdmin", "Home");
        //}
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

    }
}