using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Platform Administrator")]
    public class PlatformAdminController : Controller
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IApplicationUserRepository _userRepository;

        public PlatformAdminController(ICompanyRepository companyRepository, IApplicationUserRepository userRepository)
        {
            _companyRepository = companyRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Displays the confirmation page before deactivating a user and their companies.
        /// </summary>
        /// <param name="id">The ID of the user to deactivate.</param>
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        /// <summary>
        /// Handles the deactivation of a Company Administrator and all their associated active companies.
        /// </summary>
        /// <param name="userIdToDeactivate">The ID of the user being deactivated.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUserAndCompanies(string userIdToDeactivate)
        {
            var platformAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var userToDeactivate = await _userRepository.GetUserByIdAsync(userIdToDeactivate);

            if (userToDeactivate == null) return NotFound();

            // 1. Deactivate all companies owned by this user
            var companies = await _companyRepository.GetCompaniesByUserIdAsync(userToDeactivate.Id);
            foreach (var company in companies)
            {
                company.IsActive = false;
                company.DeletedAt = DateTime.UtcNow;
                company.UserDeletedId = platformAdmin.Id;
                _companyRepository.Update(company);
            }

            // 2. Lock the user's account permanently and update audit fields
            userToDeactivate.DeactivatedAt = DateTime.UtcNow;
            userToDeactivate.DeactivatedByUserId = platformAdmin.Id;
            await _userRepository.UpdateUserAsync(userToDeactivate);
            await _userRepository.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

            // 3. Save all changes to the company database
            await _companyRepository.SaveAllAsync();

            TempData["UserManagementSuccessMessage"] = $"User {userToDeactivate.Email} and all their companies have been deactivated.";

            return RedirectToAction("Index", "Home");
        }
    }
}