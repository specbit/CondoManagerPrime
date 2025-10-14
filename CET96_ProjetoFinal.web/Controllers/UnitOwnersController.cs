using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    public class UnitOwnersController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly IEmailSender _emailSender;

        public UnitOwnersController(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            ICondominiumRepository condominiumRepository,
            IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _condominiumRepository = condominiumRepository;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Displays a list of "Unit Owner" users for a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium whose owners are to be displayed.</param>
        public async Task<IActionResult> Index(int condominiumId)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);

            if (condominium == null) return NotFound();

            // --- SECURITY CHECK: Ensure user has permission for this condominium ---
            bool isAuthorized = (User.IsInRole("Company Administrator") && condominium.CompanyId == currentUser.CompanyId) ||
                                (User.IsInRole("Condominium Manager") && condominium.CondominiumManagerId == currentUser.Id);

            if (!isAuthorized)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var owners = await _userRepository.GetUsersInRoleByCondominiumAsync("Unit Owner", condominiumId);

            ViewBag.CondominiumId = condominium.Id;
            ViewBag.CondominiumName = condominium.Name;

            return View(owners);
        }

        /// <summary>
        /// Displays the form to create a new Unit Owner for a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium to which the new owner will belong.</param>
        public async Task<IActionResult> Create(int condominiumId)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);

            if (condominium == null) return NotFound();

            // --- SECURITY CHECK ---
            bool isAuthorized = (User.IsInRole("Company Administrator") && condominium.CompanyId == currentUser.CompanyId) ||
                                (User.IsInRole("Condominium Manager") && condominium.CondominiumManagerId == currentUser.Id);

            if (!isAuthorized)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var model = new UnitOwnerViewModel
            {
                CondominiumId = condominiumId,
                CompanyId = condominium.CompanyId
            };

            ViewBag.CondominiumName = condominium.Name;
            return View(model);
        }

        /// <summary>
        /// Handles the creation of a new Unit Owner account.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnitOwnerViewModel model)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var condominium = await _condominiumRepository.GetByIdAsync(model.CondominiumId);

            if (condominium == null)
            {
                ModelState.AddModelError("", "Selected condominium not found.");
            }

            ViewBag.CondominiumName = condominium?.Name;

            if (ModelState.IsValid)
            {
                // --- SECURITY CHECK ---
                bool isAuthorized = (User.IsInRole("Company Administrator") && condominium.CompanyId == currentUser.CompanyId) ||
                                    (User.IsInRole("Condominium Manager") && condominium.CondominiumManagerId == currentUser.Id);
                if (!isAuthorized)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email address is already in use.");
                    return View(model);
                }

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
                    CompanyId = condominium.CompanyId,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    UserCreatedId = currentUser.Id,
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Unit Owner");

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var link = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
                    await _emailSender.SendEmailAsync(user.Email, "Confirm your CondoManagerPrime Account", $"Please confirm your account by clicking this link: <a href='{link}'>link</a>");

                    TempData["StatusMessage"] = $"Unit Owner {user.FirstName} {user.LastName} created. A confirmation email has been sent.";
                    return RedirectToAction(nameof(Index), new { condominiumId = model.CondominiumId });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}