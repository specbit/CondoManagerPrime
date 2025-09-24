using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    [Route("Units")]
    public class UnitsController : Controller
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ICondominiumRepository _condominiumRepository;

        public UnitsController(IUnitRepository unitRepository, ICondominiumRepository condominiumRepository)
        {
            _unitRepository = unitRepository;
            _condominiumRepository = condominiumRepository;
        }

        // GET: Units?condominiumId=5
        /// <summary>
        /// Displays a list of all active units for a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium.</param>
        /// <param name="returnUrl">The URL to return to after an action.</param>
        /// <returns>A view with the list of units.</returns>
        [HttpGet("{condominiumId:int}")] // GET Units/1005
        public async Task<IActionResult> Index(int condominiumId, string returnUrl = null)
        {
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);
            if (condominium == null)
            {
                return NotFound();
            }

            ViewBag.CompanyId = condominium.CompanyId;
            var units = await _unitRepository.GetUnitsByCondominiumIdAsync(condominiumId);

            // Pass data to the view for navigation and display
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.CompanyId = condominium.CompanyId;
            ViewBag.CondominiumName = condominium.Name;
            ViewBag.CondominiumId = condominiumId;

            return View(units);
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
                var unit = new Unit
                {
                    UnitNumber = model.UnitNumber,
                    CondominiumId = model.CondominiumId,
                    IsActive = true
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

                unitToUpdate.UnitNumber = model.UnitNumber;
                unitToUpdate.UpdatedAt = DateTime.UtcNow;

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
        /// This action finds the specified unit, sets its IsActive flag to false,
        /// records the deletion timestamp, and saves the changes. It then redirects
        /// the user back to their original page using the provided returnUrl.
        /// </remarks>
        /// <param name="id">The ID of the unit to be deactivated.</param>
        /// <param name="returnUrl">The URL to return to after the action is complete.</param>
        /// <returns>A redirect to the returnUrl or a fallback page.</returns>
        [HttpPost("Deactivate/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnUrl = null)
        {
            var unitToDelete = await _unitRepository.GetByIdAsync(id);
            if (unitToDelete != null)
            {
                // This is a SOFT delete
                unitToDelete.IsActive = false;
                unitToDelete.DeletedAt = DateTime.UtcNow;
                // TODO: You would set UserDeletedId here if tracking it

                _unitRepository.Update(unitToDelete);
                await _unitRepository.SaveAllAsync();
                TempData["StatusMessage"] = "Unit deactivated successfully.";
            }
            else
            {
                TempData["StatusMessage"] = "Error: Unit not found.";
            }

            // --- REDIRECT LOGIC ---
            // If a returnUrl was provided, use it.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Fallback to the default unit list for that condominium if possible
            if (unitToDelete != null)
            {
                return RedirectToAction(nameof(Index), new { condominiumId = unitToDelete.CondominiumId });
            }

            // Final fallback if everything else fails
            return RedirectToAction(nameof(Index));
        }

        // GET: Units/InactiveUnits?condominiumId=5
        /// <summary>
        /// Displays a list of all inactive units for a specific condominium.
        /// </summary>
        [HttpGet("{condominiumId:int}/Inactive")] // GET Units/1005/Inactive
        public async Task<IActionResult> Inactive(int condominiumId)
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
                unitToReactivate.IsActive = true;
                unitToReactivate.DeletedAt = null;
                unitToReactivate.UpdatedAt = DateTime.UtcNow;
                // You would set UserUpdatedId here if tracking it

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
    }
}