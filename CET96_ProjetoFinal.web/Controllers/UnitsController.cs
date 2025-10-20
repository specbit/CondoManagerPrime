using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    [Route("Units")]
    public class UnitsController : Controller
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly UserManager<ApplicationUser> _userManager; //For AssignOwnerToUnit

        private readonly IEmailSender _emailSender;
        private readonly IApplicationUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;

        public UnitsController(
                    IUnitRepository unitRepository,
                    ICondominiumRepository condominiumRepository,
                    UserManager<ApplicationUser> userManager,
                    IEmailSender emailSender,
                    IApplicationUserRepository userRepository,
                    ICompanyRepository companyRepository)
        {
            _unitRepository = unitRepository;
            _condominiumRepository = condominiumRepository;
            _userManager = userManager;
            _emailSender = emailSender;
            _userRepository = userRepository;
            _companyRepository = companyRepository;
        }

        // GET: Units?condominiumId=5
        /// <summary>
        /// Displays a list of all active units for a specific condominium, including the assigned owner's name.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium.</param>
        /// <param name="returnUrl">An optional URL to return to after an action.</param>
        /// <returns>A view with a list of UnitViewModels.</returns>
        [HttpGet("{condominiumId:int}")] // GET Units/1005
        public async Task<IActionResult> Index(int condominiumId, string returnUrl = null)
        {
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);
            if (condominium == null)
            {
                return NotFound();
            }

            // 1. Get all the units for this condominium.
            var units = await _unitRepository.GetUnitsByCondominiumIdAsync(condominiumId);

            // 2. Get all the owners in this condominium just ONCE for efficiency.
            var owners = await _userRepository.GetUsersInRoleByCondominiumAsync("Unit Owner", condominiumId);

            // 3. Create the list of ViewModels to pass to the view.
            var model = new List<UnitViewModel>();
            foreach (var unit in units)
            {
                // Find the matching owner for the current unit.
                var owner = owners.FirstOrDefault(o => o.Id == unit.OwnerId);

                model.Add(new UnitViewModel
                {
                    Id = unit.Id,
                    UnitNumber = unit.UnitNumber,
                    IsActive = unit.IsActive,
                    // If an owner is found, use their name; otherwise, show "(Not Assigned)".
                    OwnerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "(Not Assigned)",
                    OwnerId = unit.OwnerId
                });
            }

            // Pass data to the view for navigation and display.
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.CondominiumName = condominium.Name;
            ViewBag.CondominiumId = condominiumId;
            ViewBag.CompanyId = condominium.CompanyId; 

            // Return the view with the new list of ViewModels.
            return View(model);
        }

        // GET: Units/Create?condominiumId=5
        [HttpGet("{condominiumId:int}/Create")] // GET Units/1005/Create
        public IActionResult Create(int condominiumId)
        {
            var model = new CreateUnitViewModel
            {
                CondominiumId = condominiumId
            };
            return View(model);
        }

        // POST: Units/Create
        /// <summary>
        /// Handles the submission of the new unit creation form.
        /// </summary>
        [HttpPost("{condominiumId:int}/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUnitViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            // First, check if a unit with this number already exists in this condominium.
            if (await _unitRepository.UnitNumberExistsAsync(model.CondominiumId, model.UnitNumber))
            {
                ModelState.AddModelError("UnitNumber", "This unit number already exists in this condominium.");
            }

            if (ModelState.IsValid)
            {
                var loggedInUserId = _userManager.GetUserId(User);

                var unit = new Unit
                {
                    UnitNumber = model.UnitNumber,
                    CondominiumId = model.CondominiumId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow, // It's good practice to set this here 
                    UserCreatedId = loggedInUserId
                };
                await _unitRepository.CreateAsync(unit);
                await _unitRepository.SaveAllAsync();

                TempData["StatusMessage"] = "Unit created successfully.";
                return RedirectToAction(nameof(Index), new { condominiumId = model.CondominiumId });
            }

            // If the model is not valid, return to the form with the errors.
            return View(model);
        }

        // GET: Units/Edit/5
        /// <summary>
        /// Displays the form to edit an existing unit.
        /// </summary>
        /// <param name="id">The ID of the unit to edit.</param>
        /// <param name="returnUrl">The URL to return to after an action.</param>
        /// <returns>The Edit Unit view.</returns>
        [HttpGet("Edit/{id:int}")] // GET Units/Edit/5
        public async Task<IActionResult> Edit(int id, string returnUrl = null)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound();

            ViewData["ReturnUrl"] = returnUrl;

            var model = new EditUnitViewModel
            {
                Id = unit.Id,
                CondominiumId = unit.CondominiumId,
                UnitNumber = unit.UnitNumber
            };
            return View(model);
        }

        // POST: Units/Edit/5
        /// <summary>
        /// Handles the submission of the unit edit form.
        /// </summary>
        /// <remarks>
        /// This action validates the model, checks for duplicate unit numbers,
        /// saves the changes, and then redirects the user back to their original
        /// page using the provided returnUrl.
        /// </remarks>
        /// <param name="model">The view model with the unit's updated details.</param>
        /// <param name="returnUrl">The URL to return to after the action is complete.</param>
        /// <returns>A redirect to the returnUrl on success, or the edit view with errors on failure.</returns>
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUnitViewModel model, string returnUrl = null)
        {
            // Preserve the returnUrl in case we need to re-display the form with errors
            ViewData["ReturnUrl"] = returnUrl;

            if (await _unitRepository.UnitNumberExistsAsync(model.CondominiumId, model.UnitNumber, model.Id))
            {
                ModelState.AddModelError("UnitNumber", "This unit number already exists in this condominium.");
            }

            if (ModelState.IsValid)
            {
                var unitToUpdate = await _unitRepository.GetByIdAsync(model.Id);
                if (unitToUpdate == null) return NotFound();

                var loggedInUserId = _userManager.GetUserId(User);

                unitToUpdate.UnitNumber = model.UnitNumber;
                unitToUpdate.UpdatedAt = DateTime.UtcNow;
                unitToUpdate.UserUpdatedId = loggedInUserId;

                _unitRepository.Update(unitToUpdate);
                await _unitRepository.SaveAllAsync();

                TempData["StatusMessage"] = "Unit updated successfully.";

                // If a returnUrl was provided, use it.
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Otherwise, use the default redirect as a fallback.
                return RedirectToAction(nameof(Index), new { condominiumId = model.CondominiumId });
            }

            return View(model);
        }

        // GET: Units/Delete/5
        /// <summary>
        /// Displays a confirmation page before deactivating a unit.
        /// </summary>
        /// <param name="id">The ID of the unit to deactivate.</param>
        /// <param name="returnUrl">The URL to return to after an action.</param>
        /// <returns>The Delete Unit confirmation view.</returns>
        [HttpGet("Deactivate/{id:int}")]
        public async Task<IActionResult> Delete(int id, string returnUrl = null)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound();

            ViewData["ReturnUrl"] = returnUrl;

            return View(unit);
        }

        // POST: Units/Delete/5
        /// <summary>
        /// Handles the confirmed deactivation (soft delete) of a unit.
        /// </summary>
        /// <remarks>
        /// This action enforces the business rule that a unit cannot be deactivated if it has an assigned owner.
        /// If the check passes, it sets the unit's IsActive flag to false, records the deletion timestamp,
        /// and saves the changes. It then redirects the user back to their original page using the provided returnUrl.
        /// </remarks>
        /// <param name="id">The ID of the unit to be deactivated.</param>
        /// <param name="returnUrl">The URL to return to after the action is complete.</param>
        /// <returns>A redirect to the returnUrl or a fallback page.</returns>
        [HttpPost("Deactivate/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnUrl = null)
        {
            var unitToDelete = await _unitRepository.GetByIdAsync(id);

            if (unitToDelete == null)
            {
                TempData["StatusMessage"] = "Error: Unit not found.";
                return RedirectToAction(nameof(Index)); // Fallback to the main index if the unit doesn't exist.
            }

            // --- Business rule check ---
            if (!string.IsNullOrEmpty(unitToDelete.OwnerId))
            {
                TempData["StatusMessage"] = "Error: Cannot deactivate a unit with an assigned owner. Please dismiss the owner first.";
                return RedirectToAction(nameof(Index), new { condominiumId = unitToDelete.CondominiumId });
            }

            var loggedInUserId = _userManager.GetUserId(User);

            // This is a SOFT delete
            unitToDelete.IsActive = false;
            unitToDelete.DeletedAt = DateTime.UtcNow;
            unitToDelete.UserDeletedId = loggedInUserId;

            _unitRepository.Update(unitToDelete);
            await _unitRepository.SaveAllAsync();
            TempData["StatusMessage"] = "Unit deactivated successfully.";

            // --- REDIRECT LOGIC ---
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Fallback to the default unit list for that condominium.
            return RedirectToAction(nameof(Index), new { condominiumId = unitToDelete.CondominiumId });
        }

        /// <summary>
        /// Displays a detailed view of a single unit, including its condominium and owner details.
        /// </summary>
        /// <param name="id">The ID of the unit to display.</param>
        /// <returns>The details view for the specified unit.</returns>
        [HttpGet("Details/{id:int}")] // GET Units/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // Step 1: Get the Unit and its Condominium from the first database.
            var unit = await _unitRepository.GetUnitWithDetailsAsync(id);

            if (unit == null)
            {
                return NotFound();
            }

            // Step 2: Manually fetch the Owner from the second database.
            // Check if there is an OwnerId to look up.
            if (!string.IsNullOrEmpty(unit.OwnerId))
            {
                // Use the user repository to get the user from the other database context.
                var owner = await _userRepository.GetUserByIdAsync(unit.OwnerId);

                // Manually "stitch" the owner object onto our unit object in C# code.
                unit.Owner = owner;
            }

            return View(unit);
        }

        // GET: Units/InactiveUnits?condominiumId=5
        /// <summary>
        /// Displays a list of all inactive units for a specific condominium.
        /// </summary>
        [HttpGet("{condominiumId:int}/Inactive")] 
        public async Task<IActionResult> InactiveUnits(int condominiumId)
        {
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);
            if (condominium == null) return NotFound();

            // Fetch only the INACTIVE units for this condominium
            var allUnits = await _unitRepository.GetAllAsync();

            var inactiveUnits = allUnits
                .Where(u => u.CondominiumId == condominiumId && !u.IsActive);

            ViewBag.CondominiumId = condominiumId;
            ViewBag.CondominiumName = condominium.Name;

            return View(inactiveUnits);
        }

        // POST: Units/Reactivate/5
        /// <summary>
        /// Reactivates a previously soft-deleted unit.
        /// </summary>
        [HttpPost("Reactivate/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var unitToReactivate = await _unitRepository.GetByIdAsync(id);

            if (unitToReactivate != null)
            {
                var loggedInUserId = _userManager.GetUserId(User);

                unitToReactivate.IsActive = true;
                unitToReactivate.DeletedAt = null;       // Clear deletion date
                unitToReactivate.UserDeletedId = null;   // Clear who deleted it
                unitToReactivate.UpdatedAt = DateTime.UtcNow;
                unitToReactivate.UserUpdatedId = loggedInUserId;

                _unitRepository.Update(unitToReactivate);
                await _unitRepository.SaveAllAsync();
                TempData["StatusMessage"] = "Unit reactivated successfully.";

                // Redirect back to the inactive list to see the change
                return RedirectToAction(nameof(Index), new { condominiumId = unitToReactivate.CondominiumId });
            }

            TempData["StatusMessage"] = "Error: Unit not found.";
            // Fallback redirect in case of an error
            return RedirectToAction(nameof(Index));
        }

        // GET: Units/AssignOwner/5
        /// <summary>
        /// Displays the form to assign an unassigned owner to a specific unit.
        /// </summary>
        /// <param name="id">The ID of the Unit.</param>
        [HttpGet("AssignOwner/{id:int}")]
        public async Task<IActionResult> AssignOwner(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound();
            var condo = await _condominiumRepository.GetByIdAsync(unit.CondominiumId);

            var allOwners = await _userManager.GetUsersInRoleAsync("Unit Owner");

            // Find owners in this condo who are not yet assigned to any unit
            //var availableOwners = allOwners.Where(o => o.CondominiumId == unit.CondominiumId && !_unitRepository.IsOwnerAssigned(o.Id));

            // Allow assigning an owner even if they already own other units
            // (filter only by same condominium; remove the IsOwnerAssigned(...) exclusion)
            var availableOwners = allOwners
                .Where(o => o.CondominiumId == unit.CondominiumId);
            var model = new AssignOwnerToUnitViewModel
            {
                UnitId = unit.Id,
                UnitNumber = unit.UnitNumber,
                CondominiumName = condo.Name,
                AvailableOwners = availableOwners.Select(o => new SelectListItem
                {
                    Value = o.Id,
                    Text = $"{o.FirstName} {o.LastName} ({o.Email})"
                })
            };

            ViewBag.CondominiumId = unit.CondominiumId;
            return View(model);
        }

        // POST: Units/AssignOwner
        /// <summary>
        /// Handles the form submission to assign an owner to a unit and sends notification emails.
        /// </summary>
        [HttpPost("AssignOwner/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignOwner([Bind("UnitId", "UnitNumber", "CondominiumName", "SelectedOwnerId")] AssignOwnerToUnitViewModel model)
        {
            var unit = await _unitRepository.GetByIdAsync(model.UnitId);
            if (unit == null) return NotFound();

            // The dropdown list must be re-populated BEFORE checking ModelState.IsValid.
            // This satisfies the model binder and prevents the hidden "AvailableOwners is required" error.
            var allOwners = await _userManager.GetUsersInRoleAsync("Unit Owner");

            // Original filtering logic (commented out) uncomment to restrict to unassigned owners only
            //var availableOwners = allOwners.Where(o => o.CondominiumId == unit.CondominiumId && !_unitRepository.IsOwnerAssigned(o.Id));

            // Allow assigning an owner even if they already own other units
            // (filter only by same condominium; remove the IsOwnerAssigned(...) exclusion)
            var availableOwners = allOwners
                .Where(o => o.CondominiumId == unit.CondominiumId);
            model.AvailableOwners = availableOwners.Select(o => new SelectListItem
                {
                    Value = o.Id,
                    Text = $"{o.FirstName} {o.LastName} ({o.Email})"
                });

            if (ModelState.IsValid)
            {
                // --- DATABASE UPDATE ---
                unit.OwnerId = model.SelectedOwnerId;
                _unitRepository.Update(unit);
                await _unitRepository.SaveAllAsync();

                // --- START: EMAIL NOTIFICATION LOGIC ---
                var owner = await _userRepository.GetUserByIdAsync(model.SelectedOwnerId);
                var condo = await _condominiumRepository.GetByIdAsync(unit.CondominiumId);

                // 1. Email to the Owner being assigned
                await _emailSender.SendEmailAsync(owner.Email,
                    $"You have been assigned to Unit {unit.UnitNumber}",
                    $"<p>Hello {owner.FirstName},</p><p>This is a notification that you have been assigned as the owner of <b>Unit {unit.UnitNumber}</b> in the condominium <b>{condo.Name}</b>.</p>");

                // 2. Email to the Condominium Manager
                if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                {
                    var condoManager = await _userRepository.GetUserByIdAsync(condo.CondominiumManagerId);
                    if (condoManager != null)
                    {
                        await _emailSender.SendEmailAsync(condoManager.Email,
                            $"Owner Assigned: Unit {unit.UnitNumber}",
                            $"<p>This is a notification that {owner.FirstName} {owner.LastName} has been assigned as the owner of <b>Unit {unit.UnitNumber}</b>.</p>");
                    }
                }

                // 3. Email to the Company Administrator
                var company = await _companyRepository.GetByIdAsync(condo.CompanyId);
                var companyAdmin = await _userRepository.GetUserByIdAsync(company.ApplicationUserId);
                if (companyAdmin != null)
                {
                    await _emailSender.SendEmailAsync(companyAdmin.Email,
                       $"Owner Assigned in {condo.Name}",
                       $"<p>An owner assignment has been made in <b>{condo.Name}</b>:</p><ul><li>Owner: {owner.FirstName} {owner.LastName}</li><li>Unit: {unit.UnitNumber}</li></ul>");
                }
                // --- END: EMAIL NOTIFICATION LOGIC ---

                TempData["StatusMessage"] = $"Owner '{owner.FirstName} {owner.LastName}' successfully assigned to Unit {unit.UnitNumber}. Notifications have been sent.";
                return RedirectToAction(nameof(Index), new { condominiumId = unit.CondominiumId });
            }

            // If ModelState is invalid (e.g., no owner was selected), re-populate the other view model properties and return.
            var failedCondo = await _condominiumRepository.GetByIdAsync(unit.CondominiumId);
            model.CondominiumName = failedCondo.Name;
            model.UnitNumber = unit.UnitNumber;
            ViewBag.CondominiumId = unit.CondominiumId;

            return View(model);
        }

        /// <summary>
        /// Displays a confirmation page before dismissing an owner from a unit.
        /// </summary>
        /// <param name="id">The ID of the Unit.</param>
        [HttpGet("DismissOwner/{id:int}")]
        public async Task<IActionResult> DismissOwner(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null || string.IsNullOrEmpty(unit.OwnerId))
            {
                return NotFound(); // Can't dismiss if there's no owner
            }

            var owner = await _userRepository.GetUserByIdAsync(unit.OwnerId);
            ViewBag.OwnerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "the current owner";
            ViewBag.CondominiumId = unit.CondominiumId;

            return View(unit);
        }

        /// <summary>
        /// Handles the confirmed dismissal of an owner from a unit and sends notification emails.
        /// </summary>
        /// <param name="id">The ID of the unit from which the owner will be dismissed.</param>
        [HttpPost("DismissOwner/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissOwnerConfirmed(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null || string.IsNullOrEmpty(unit.OwnerId))
            {
                return NotFound();
            }

            // Get the owner's details BEFORE we dismiss them, so we know who they were.
            var owner = await _userRepository.GetUserByIdAsync(unit.OwnerId);

            // --- DATABASE UPDATE ---
            // Set the OwnerId to null to dismiss the owner.
            unit.OwnerId = null;
            _unitRepository.Update(unit);
            await _unitRepository.SaveAllAsync();

            // --- START: EMAIL NOTIFICATION LOGIC ---
            if (owner != null) // Only send emails if we found the owner's details
            {
                var condo = await _condominiumRepository.GetByIdAsync(unit.CondominiumId);

                // 1. Email to the Owner who was dismissed
                await _emailSender.SendEmailAsync(owner.Email,
                    $"You have been dismissed from Unit {unit.UnitNumber}",
                    $"<p>Hello {owner.FirstName},</p><p>This is a notification that you are no longer assigned as the owner of <b>Unit {unit.UnitNumber}</b> in the condominium <b>{condo.Name}</b>.</p>");

                // 2. Email to the Condominium Manager
                if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                {
                    var condoManager = await _userRepository.GetUserByIdAsync(condo.CondominiumManagerId);
                    if (condoManager != null)
                    {
                        await _emailSender.SendEmailAsync(condoManager.Email,
                            $"Owner Dismissed: Unit {unit.UnitNumber}",
                            $"<p>This is a notification that {owner.FirstName} {owner.LastName} has been dismissed as the owner of <b>Unit {unit.UnitNumber}</b>.</p>");
                    }
                }

                // 3. Email to the Company Administrator
                var company = await _companyRepository.GetByIdAsync(condo.CompanyId);
                var companyAdmin = await _userRepository.GetUserByIdAsync(company.ApplicationUserId);
                if (companyAdmin != null)
                {
                    await _emailSender.SendEmailAsync(companyAdmin.Email,
                       $"Owner Dismissed in {condo.Name}",
                       $"<p>An owner dismissal has occurred in <b>{condo.Name}</b>:</p><ul><li>Former Owner: {owner.FirstName} {owner.LastName}</li><li>Unit: {unit.UnitNumber}</li></ul>");
                }
            }
            // --- END: EMAIL NOTIFICATION LOGIC ---

            TempData["StatusMessage"] = $"Owner '{owner?.FirstName} {owner?.LastName}' was successfully dismissed from Unit {unit.UnitNumber}. Notifications sent.";
            return RedirectToAction(nameof(Index), new { condominiumId = unit.CondominiumId });
        }
    }
}