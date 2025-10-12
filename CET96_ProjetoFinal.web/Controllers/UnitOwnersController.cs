using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    /// <summary>
    /// Manages CRUD operations for "Unit Owner" users.
    /// </summary>
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    public class UnitOwnersController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly IEmailSender _emailSender;
        private readonly ICompanyRepository _companyRepository;

        public UnitOwnersController(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            ICondominiumRepository condominiumRepository,
            IEmailSender emailSender,
            ICompanyRepository companyRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _condominiumRepository = condominiumRepository;
            _emailSender = emailSender;
            _companyRepository = companyRepository;
        }

        /// <summary>
        /// Displays a list of all users who have the "Unit Owner" role.
        /// </summary>
        /// <returns>A view containing the list of unit owners.</returns>
        public async Task<IActionResult> Index()
        {
            var owners = await _userManager.GetUsersInRoleAsync("Unit Owner");
            return View(owners);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var model = new UnitOwnerViewModel { CompanyId = currentUser.CompanyId.Value };

            if (User.IsInRole("Company Administrator"))
            {
                var condos = await _condominiumRepository.GetActiveCondominiumsByCompanyIdAsync(currentUser.CompanyId.Value);
                model.CondominiumsList = condos.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
                model.CanPickCondominium = true;
            }
            else // User is a Condominium Manager
            {
                var assignedCondo = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
                if (assignedCondo == null)
                {
                    TempData["StatusMessage"] = "Error: You must be assigned to a condominium to create an owner.";
                    return RedirectToAction(nameof(Index));
                }
                model.CondominiumsList = new[] { new SelectListItem { Value = assignedCondo.Id.ToString(), Text = assignedCondo.Name } };
                model.CondominiumId = assignedCondo.Id;
                model.CanPickCondominium = false;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnitOwnerViewModel model)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Re-populate dropdowns first, so they are always available if validation fails.
            if (User.IsInRole("Company Administrator"))
            {
                var condos = await _condominiumRepository.GetActiveCondominiumsByCompanyIdAsync(currentUser.CompanyId.Value);
                model.CondominiumsList = condos.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
                model.CanPickCondominium = true;
            }
            else // Manager
            {
                var assignedCondo = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
                model.CondominiumsList = new[] { new SelectListItem { Value = assignedCondo.Id.ToString(), Text = assignedCondo.Name } };
                model.CanPickCondominium = false;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // --- Security & Validation (mirrors your Staff controller) ---
            var selectedCondo = await _condominiumRepository.GetByIdAsync(model.CondominiumId);
            if (selectedCondo == null || selectedCondo.CompanyId != currentUser.CompanyId)
            {
                return RedirectToAction("AccessDenied", "Account"); // Attempting to create owner in wrong company
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email address is already in use.");
                return View(model);
            }
            // --- End Security & Validation ---

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                IdentificationDocument = model.IdentificationDocument,
                DocumentType = model.DocumentType,
                CondominiumId = model.CondominiumId,
                CompanyId = selectedCondo.CompanyId,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UserCreatedId = currentUser.Id,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Unit Owner");

                // --- Email Logic (mirrors your Staff controller) ---
                // 1. Send confirmation to new owner
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
                await _emailSender.SendEmailAsync(user.Email, "Confirm your CondoManagerPrime Account",
                    $"Please confirm your account by clicking this link: <a href='{link}'>link</a>");

                // 2. Send notification to the creator (Admin or Manager)
                await _emailSender.SendEmailAsync(currentUser.Email, $"New Unit Owner Created: {user.FirstName} {user.LastName}",
                    $"You have successfully created a new unit owner account for {user.FirstName} {user.LastName} in the condominium {selectedCondo.Name}.");

                TempData["StatusMessage"] = $"Unit Owner {user.FirstName} {user.LastName} created. A confirmation email has been sent.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}