using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    public class ManageOwnersController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly IEmailSender _emailSender;
        private readonly ICompanyRepository _companyRepository; // This is needed for the correct security check
        private readonly IUnitRepository _unitRepository;


        public ManageOwnersController(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            ICondominiumRepository condominiumRepository,
            IEmailSender emailSender,
            ICompanyRepository companyRepository,
        IUnitRepository unitRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _condominiumRepository = condominiumRepository;
            _emailSender = emailSender;
            _companyRepository = companyRepository;
            _unitRepository = unitRepository;
        }

        /// <summary>
        /// Displays a list of "Unit Owner" users for a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium whose owners are to be displayed.</param>
        public async Task<IActionResult> Index(int condominiumId)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (currentUser == null) return Forbid();

            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);
            if (condominium == null) return NotFound();

            // --- SECURITY CHECK ---
            bool isAuthorized = false;
            if (User.IsInRole("Company Administrator"))
            {
                // For an Admin, check if the requested condominium's CompanyId is in the list of companies they manage.
                var managedCompanies = await _companyRepository.GetCompaniesByUserIdAsync(currentUser.Id);
                if (managedCompanies.Any(c => c.Id == condominium.CompanyId))
                {
                    isAuthorized = true;
                }
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                // For a Manager, check if they are directly assigned to this condo.
                if (condominium.CondominiumManagerId == currentUser.Id)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            // --- END SECURITY CHECK ---

            // --- NEW LOGIC TO BUILD THE VIEWMODEL ---
            // 1. Get all owners for this condominium.
            var ownersInCondo = await _userRepository.GetUsersInRoleByCondominiumAsync("Unit Owner", condominiumId);

            // 2. Create the list of ViewModels to pass to the view.
            var model = new List<UnitOwnerListViewModel>();
            foreach (var owner in ownersInCondo)
            {
                model.Add(new UnitOwnerListViewModel
                {
                    Id = owner.Id,
                    FullName = $"{owner.FirstName} {owner.LastName}",
                    Email = owner.Email,
                    PhoneNumber = owner.PhoneNumber,
                    IsActive = !owner.DeactivatedAt.HasValue,
                    IsConfirmed = owner.EmailConfirmed,
                    // 3. Check if this owner is assigned to any unit.
                    IsAssigned = _unitRepository.IsOwnerAssigned(owner.Id)
                });
            }
            // --- END NEW LOGIC ---

            ViewBag.CondominiumId = condominium.Id;
            ViewBag.CondominiumName = condominium.Name;

            // Return the view with the new, richer list of ViewModels.
            return View(model);
        }

        /// <summary>
        /// Displays the form to create a new Unit Owner for a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium to which the new owner will belong.</param>
        public async Task<IActionResult> Create(int condominiumId)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (currentUser == null) return Forbid();

            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);
            if (condominium == null) return NotFound();

            // --- SECURITY CHECK ---
            bool isAuthorized = false;
            if (User.IsInRole("Company Administrator"))
            {
                // For an Admin, check if the requested condominium's CompanyId is in the list of companies they manage.

                var managedCompanies = await _companyRepository.GetCompaniesByUserIdAsync(currentUser.Id);
                if (managedCompanies.Any(c => c.Id == condominium.CompanyId))
                {
                    isAuthorized = true;
                }
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                // For a Manager, check if they are directly assigned to this condo.

                if (condominium.CondominiumManagerId == currentUser.Id)
                {
                    isAuthorized = true;
                }
            }

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
            if (currentUser == null) return Forbid();

            // Check if another user already exists with this ID document number.
            bool documentExists = await _userManager.Users.AnyAsync(u => u.IdentificationDocument == model.IdentificationDocument);
            if (documentExists)
            {
                ModelState.AddModelError("IdentificationDocument", "An owner with this Identification Document number already exists.");
            }

            var condominium = await _condominiumRepository.GetByIdAsync(model.CondominiumId);
            if (condominium == null)
            {
                ModelState.AddModelError("", "Selected condominium not found.");
            }

            ViewBag.CondominiumName = condominium?.Name;

            if (ModelState.IsValid)
            {
                // --- SECURITY CHECK ---
                bool isAuthorized = false;
                if (User.IsInRole("Company Administrator"))
                {
                    // For an Admin, check if the requested condominium's CompanyId is in the list of companies they manage.

                    var managedCompanies = await _companyRepository.GetCompaniesByUserIdAsync(currentUser.Id);
                    if (managedCompanies.Any(c => c.Id == condominium.CompanyId))
                    {
                        isAuthorized = true;
                    }
                }
                else if (User.IsInRole("Condominium Manager"))
                {
                    // For a Manager, check if they are directly assigned to this condo.

                    if (condominium.CondominiumManagerId == currentUser.Id)
                    {
                        isAuthorized = true;
                    }
                }

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

        /// <summary>
        /// Displays the form to edit an existing unit owner.
        /// </summary>
        /// <param name="id">The ID of the owner to edit.</param>
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var owner = await _userRepository.GetUserByIdAsync(id);
            if (owner == null) return NotFound();

            // --- Security Check (same as Details action) ---
            var condominium = await _condominiumRepository.GetByIdAsync(owner.CondominiumId.Value);
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool isAuthorized = false;
            if (User.IsInRole("Company Administrator"))
            {
                var managedCompanies = await _companyRepository.GetCompaniesByUserIdAsync(currentUser.Id);
                if (managedCompanies.Any(c => c.Id == condominium.CompanyId)) isAuthorized = true;
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                if (condominium.CondominiumManagerId == currentUser.Id) isAuthorized = true;
            }
            if (!isAuthorized) return RedirectToAction("AccessDenied", "Account");

            // Populate the ViewModel from the user entity
            var model = new EditUnitOwnerViewModel
            {
                Id = owner.Id,
                FirstName = owner.FirstName,
                LastName = owner.LastName,
                PhoneNumber = owner.PhoneNumber,
                Email = owner.Email, // Pass email for display, but it won't be editable
                IdentificationDocument = owner.IdentificationDocument,
                DocumentType = owner.DocumentType,
                CondominiumId = owner.CondominiumId.Value
            };

            ViewBag.CondominiumName = condominium.Name;
            return View(model);
        }

        /// <summary>
        /// Handles the submission of the edit owner form.
        /// </summary>
        /// <param name="model">The view model with the updated owner details.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUnitOwnerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ownerToUpdate = await _userRepository.GetUserByIdAsync(model.Id);
                if (ownerToUpdate == null) return NotFound();

                // Check if another user (not the one being edited) has this ID document number.
                bool documentExists = await _userManager.Users.AnyAsync(u => u.IdentificationDocument == model.IdentificationDocument && u.Id != model.Id);
                if (documentExists)
                {
                    ModelState.AddModelError("IdentificationDocument", "This Identification Document number is already in use by another owner.");
                }

                // --- Security Check (essential on POST as well) ---
                var condominium = await _condominiumRepository.GetByIdAsync(ownerToUpdate.CondominiumId.Value);
                var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAuthorized = false;
                if (User.IsInRole("Company Administrator"))
                {
                    var managedCompanies = await _companyRepository.GetCompaniesByUserIdAsync(currentUser.Id);
                    if (managedCompanies.Any(c => c.Id == condominium.CompanyId)) isAuthorized = true;
                }
                else if (User.IsInRole("Condominium Manager"))
                {
                    if (condominium.CondominiumManagerId == currentUser.Id) isAuthorized = true;
                }
                if (!isAuthorized) return RedirectToAction("AccessDenied", "Account");

                // Update the user's properties
                ownerToUpdate.FirstName = model.FirstName;
                ownerToUpdate.LastName = model.LastName;
                ownerToUpdate.PhoneNumber = model.PhoneNumber;
                ownerToUpdate.IdentificationDocument = model.IdentificationDocument;
                ownerToUpdate.DocumentType = model.DocumentType;
                ownerToUpdate.UpdatedAt = DateTime.UtcNow;
                ownerToUpdate.UserUpdatedId = currentUser.Id;

                await _userRepository.UpdateUserAsync(ownerToUpdate);

                TempData["StatusMessage"] = $"Owner {ownerToUpdate.FirstName} {ownerToUpdate.LastName} updated successfully.";
                return RedirectToAction(nameof(Index), new { condominiumId = ownerToUpdate.CondominiumId.Value });
            }

            // If validation fails, re-display the form
            ViewBag.CondominiumName = (await _condominiumRepository.GetByIdAsync(model.CondominiumId))?.Name;
            return View(model);
        }

        /// <summary>
        /// Displays a read-only details view for a specific unit owner.
        /// </summary>
        /// <param name="id">The string ID (GUID) of the owner to display.</param>
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var owner = await _userRepository.GetUserByIdAsync(id);
            if (owner == null || !owner.CondominiumId.HasValue)
            {
                return NotFound(); // User is not an owner or not linked to a condo
            }

            var condominium = await _condominiumRepository.GetByIdAsync(owner.CondominiumId.Value);
            var currentUser = await _userRepository.GetUserByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // --- Security Check: Ensures user has permission for this owner's condominium ---
            bool isAuthorized = false;
            if (User.IsInRole("Company Administrator"))
            {
                var managedCompanies = await _companyRepository.GetCompaniesByUserIdAsync(currentUser.Id);
                if (managedCompanies.Any(c => c.Id == condominium.CompanyId)) isAuthorized = true;
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                if (condominium.CondominiumManagerId == currentUser.Id) isAuthorized = true;
            }

            if (!isAuthorized) return RedirectToAction("AccessDenied", "Account");

            // Get Condo and Company names for display
            var company = await _companyRepository.GetByIdAsync(condominium.CompanyId);
            ViewBag.CondominiumName = condominium.Name;
            ViewBag.CompanyName = company?.Name ?? "N/A";

            // --- Intelligent "Back" Button Logic ---
            // Sets the correct return URL based on the user's role.
            if (User.IsInRole("Company Administrator"))
            {
                ViewBag.ReturnUrl = Url.Action("CondominiumDashboard", "Condominiums", new { id = owner.CondominiumId.Value });
            }
            else // Condominium Manager
            {
                ViewBag.ReturnUrl = Url.Action("Index", "Home");
            }

            return View(owner);
        }

        /// <summary>
        /// Handles the POST request to deactivate a Unit Owner's account.
        /// Enforces the business rule that an owner cannot be deactivated if they are currently assigned to a unit.
        /// </summary>
        /// <param name="id">The string identifier (GUID) of the Unit Owner to deactivate.</param>
        /// <returns>A redirect to the Index action, displaying the list of unit owners.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateOwner(string id)
        {
            // Business Rule: An owner cannot be deactivated if they are assigned to a unit.
            if (_unitRepository.IsOwnerAssigned(id))
            {
                TempData["StatusMessage"] = "Error: Cannot deactivate an owner who is currently assigned to a unit. Please dismiss them from the unit first.";

                // Determine the correct redirect based on the user's role to maintain context.
                if (User.IsInRole("Company Administrator"))
                {
                    var ownerUser = await _userRepository.GetUserByIdAsync(id);
                    return RedirectToAction(nameof(Index), new { companyId = ownerUser.CompanyId });
                }
                return RedirectToAction(nameof(Index));
            }

            var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
            if (userToDeactivate == null)
            {
                return NotFound();
            }

            var loggedInUserId = _userManager.GetUserId(User);

            // Deactivate the user by setting the audit fields.
            userToDeactivate.DeactivatedAt = DateTime.UtcNow;
            userToDeactivate.DeactivatedByUserId = loggedInUserId;
            userToDeactivate.UpdatedAt = DateTime.UtcNow;
            userToDeactivate.UserUpdatedId = loggedInUserId;

            var result = await _userManager.UpdateAsync(userToDeactivate);
            if (result.Succeeded)
            {
                // Set a lockout date in the distant future to prevent the user from logging in.
                await _userManager.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);
                TempData["StatusMessage"] = $"Owner '{userToDeactivate.FirstName} {userToDeactivate.LastName}' has been successfully deactivated.";
            }
            else
            {
                TempData["StatusMessage"] = "Error: Could not deactivate the owner.";
            }

            // Redirect back to the correct list view depending on the logged-in user's role.
            if (User.IsInRole("Company Administrator"))
            {
                return RedirectToAction(nameof(Index), new { companyId = userToDeactivate.CompanyId });
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Handles the POST request to activate a previously deactivated Unit Owner's account.
        /// </summary>
        /// <param name="id">The string identifier (GUID) of the Unit Owner to activate.</param>
        /// <returns>A redirect to the Index action, displaying the list of unit owners.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateOwner(string id)
        {
            var userToActivate = await _userRepository.GetUserByIdAsync(id);
            if (userToActivate == null)
            {
                return NotFound();
            }

            var loggedInUserId = _userManager.GetUserId(User);

            // Activate the user by clearing the deactivation audit fields.
            userToActivate.DeactivatedAt = null;
            userToActivate.DeactivatedByUserId = null;
            userToActivate.UpdatedAt = DateTime.UtcNow;
            userToActivate.UserUpdatedId = loggedInUserId;

            var result = await _userManager.UpdateAsync(userToActivate);
            if (result.Succeeded)
            {
                // Remove the lockout to allow the user to log in again.
                await _userManager.SetLockoutEndDateAsync(userToActivate, null);
                TempData["StatusMessage"] = $"Owner '{userToActivate.FirstName} {userToActivate.LastName}' has been successfully activated.";
            }
            else
            {
                TempData["StatusMessage"] = "Error: Could not activate the owner.";
            }

            // Redirect back to the correct list view.
            if (User.IsInRole("Company Administrator"))
            {
                return RedirectToAction(nameof(Index), new { companyId = userToActivate.CompanyId });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}