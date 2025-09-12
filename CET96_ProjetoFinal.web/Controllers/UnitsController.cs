using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
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
        public async Task<IActionResult> Index(int condominiumId)
        {
            var units = await _unitRepository.GetUnitsByCondominiumIdAsync(condominiumId);
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);

            ViewBag.CondominiumId = condominiumId;
            ViewBag.CondominiumName = condominium?.Name;

            return View(units);
        }

        // GET: Units/Create?condominiumId=5
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
        /// <remarks>
        /// This action validates the submitted model, checks for duplicate unit numbers within the
        /// same condominium, and saves the new unit to the database if validation is successful.
        /// </remarks>
        /// <param name="model">The view model containing the new unit's details.</param>
        /// <returns>A redirect to the unit list on success, or the create view with errors on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUnitViewModel model)
        {
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
        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound();

            var model = new EditUnitViewModel
            {
                Id = unit.Id,
                CondominiumId = unit.CondominiumId,
                UnitNumber = unit.UnitNumber
            };
            return View(model);
        }

        // POST: Units/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUnitViewModel model)
        {
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
                return RedirectToAction(nameof(Index), new { condominiumId = model.CondominiumId });
            }
            return View(model);
        }

        // GET: Units/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null) return NotFound();

            return View(unit);
        }

        // POST: Units/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
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
                return RedirectToAction(nameof(Index), new { condominiumId = unitToDelete.CondominiumId });
            }

            TempData["StatusMessage"] = "Error: Unit not found.";
            return RedirectToAction(nameof(Index)); 
        }

        // GET: Units/InactiveUnits?condominiumId=5
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
        [HttpPost]
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