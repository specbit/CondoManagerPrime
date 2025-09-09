using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class CondominiumsController : Controller
    {
        private readonly ICondominiumRepository _repository;

        public CondominiumsController(ICondominiumRepository repository)
        {
            _repository = repository;
        }

        // GET: Condominiums for a specific Company
        public async Task<IActionResult> Index(int id) // 'id' is the CompanyId
        {
            // Fetch only the condominiums for the given company
            var condominiums = await _repository.GetActiveCondominiumsByCompanyIdAsync(id);

            ViewBag.CompanyId = id; // Pass the Company for use in the view

            return View(condominiums);
        }

        // GET: Condominiums/Details/5
        public async Task<IActionResult> Details(int? id, bool fromInactive = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var condominium = await _repository.GetByIdAsync(id.Value);

            if (condominium == null)
            {
                return NotFound();
            }

            // Map the entity to our new ViewModel
            var model = new CondominiumDetailsViewModel
            {
                Id = condominium.Id,
                CompanyId = condominium.CompanyId,
                Name = condominium.Name,
                Address = condominium.Address,
                City = condominium.City,
                ZipCode = condominium.ZipCode,
                PropertyRegistryNumber = condominium.PropertyRegistryNumber,
                UnitsCount = condominium.Units.Count(),
                ContractValue = condominium.ContractValue,
                FeePerUnit = condominium.FeePerUnit,
                CreatedAt = condominium.CreatedAt.ToLocalTime(), // Convert from UTC for display
                IsActive = condominium.IsActive // Include the active status for smarter UI decisions
            };

            // Pass the flag to the view
            ViewBag.FromInactive = fromInactive;

            return View(model);
        }

        // GET: Condominiums/Create
        public IActionResult Create(int id) // 'id' will be the CompanyId
        {
            var model = new CondominiumViewModel { CompanyId = id };
            return View(model);
        }

        // POST: Condominiums/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CondominiumViewModel model)
        {
            // Check if the address is already in use for this company.
            if (await _repository.AddressExistsAsync(model.CompanyId, model.Address))
            {
                ModelState.AddModelError("Address", "This address is already registered.");
            }

            // Check if the Property Registry Number is already in use for this company.
            if (await _repository.RegistryNumberExistsAsync(model.CompanyId, model.PropertyRegistryNumber))
            {
                ModelState.AddModelError("PropertyRegistryNumber", "This Property Registry Number is already in use.");
            }

            if (ModelState.IsValid)
            {
                //// Server-side check for division by zero
                //if (model.NumberOfUnits <= 0)
                //{
                //    ModelState.AddModelError("NumberOfUnits", "Number of Units must be greater than zero.");
                //    return View(model);
                //}

                var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var newCondominium = new Condominium
                {
                    CompanyId = model.CompanyId,
                    Name = model.Name,
                    Address = model.Address,
                    City = model.City,
                    ZipCode = model.ZipCode,
                    PropertyRegistryNumber = model.PropertyRegistryNumber,
                    ContractValue = model.ContractValue,
                    // Secure server-side calculation
                    FeePerUnit = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserCreatedId = loggedInUserId
                };

                await _repository.CreateAsync(newCondominium);
                await _repository.SaveAllAsync();

                TempData["StatusMessage"] = "Condominium created successfully.";
                return RedirectToAction(nameof(Index), new { id = model.CompanyId });
            }

            ViewData["CompanyId"] = model.CompanyId;

            return View(model);
        }

        // GET: Condominiums/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var condominium = await _repository.GetByIdAsync(id.Value);

            if (condominium == null)
            {
                return NotFound();
            }

            var model = new CondominiumViewModel
            {
                Id = condominium.Id,
                CompanyId = condominium.CompanyId,
                Name = condominium.Name,
                Address = condominium.Address,
                City = condominium.City,
                ZipCode = condominium.ZipCode,
                PropertyRegistryNumber = condominium.PropertyRegistryNumber,
                ContractValue = condominium.ContractValue,
                //FeePerUnit = condominium.FeePerUnit
            };

            return View(model);
        }

        // POST: Condominiums/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CondominiumViewModel model)
        {
            if (id != model.Id) return NotFound();

            // Check if address exists for another condominium in this company.
            if (await _repository.AddressExistsAsync(model.CompanyId, model.Address, model.Id))
            {
                ModelState.AddModelError("Address", "This address is already registered for another condominium.");
            }

            // Check if registry number exists for another condominium in this company.
            if (await _repository.RegistryNumberExistsAsync(model.CompanyId, model.PropertyRegistryNumber, model.Id))
            {
                ModelState.AddModelError("PropertyRegistryNumber", "This Property Registry Number is already in use.");
            }

            if (ModelState.IsValid)
            {
                //// Server-side check for division by zero
                //if (model.NumberOfUnits <= 0)
                //{
                //    ModelState.AddModelError("NumberOfUnits", "Number of Units must be greater than zero.");
                //    return View(model);
                //}

                // 1. Fetch the original condominium from the database.
                var condominiumToUpdate = await _repository.GetByIdAsync(id);
                if (condominiumToUpdate == null) return NotFound();

                // 2. Manually update the properties. This prevents over-posting.
                condominiumToUpdate.Name = model.Name;
                condominiumToUpdate.Address = model.Address;
                condominiumToUpdate.City = model.City;
                condominiumToUpdate.ZipCode = model.ZipCode;
                condominiumToUpdate.PropertyRegistryNumber = model.PropertyRegistryNumber;
                //condominiumToUpdate.NumberOfUnits = model.NumberOfUnits;
                condominiumToUpdate.ContractValue = model.ContractValue;

                // 3. Perform the secure server-side calculation.
                //condominiumToUpdate.FeePerUnit = model.ContractValue / model.NumberOfUnits;

                // 4. Update audit fields.
                condominiumToUpdate.UpdatedAt = DateTime.UtcNow;
                condominiumToUpdate.UserUpdatedId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                try
                {
                    _repository.Update(condominiumToUpdate);
                    await _repository.SaveAllAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _repository.ExistsAsync(model.Id)) return NotFound();
                    else throw;
                }

                TempData["StatusMessage"] = "Condominium updated successfully.";
                return RedirectToAction(nameof(Index), new { id = model.CompanyId });
            }
            return View(model);
        }

        // GET: Condominiums/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var condominium = await _repository.GetByIdAsync(id.Value);

            if (condominium == null)
            {
                return NotFound();
            }

            return View(condominium);
        }

        // POST: Condominiums/Delete/5 (This now performs the SOFT delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var condominium = await _repository.GetByIdAsync(id);

            if (condominium != null)
            {
                // 1. Set the flag to inactive instead of removing the record.
                condominium.IsActive = false;

                // 2. Set the audit fields for the soft delete.
                condominium.UserDeletedId = User.FindFirstValue(ClaimTypes.NameIdentifier); // ID of the user performing the deletion
                condominium.DeletedAt = DateTime.UtcNow;

                _repository.Update(condominium);
                await _repository.SaveAllAsync();

                TempData["StatusMessage"] = "Condominium deactivated successfully.";
            }

            return RedirectToAction(nameof(Index), new { id = condominium?.CompanyId });
        }

        /// <summary>
        /// Displays a list of all inactive (soft-deleted) condominiums for a company.
        /// </summary>
        /// <param name="id">The ID of the company.</param>
        /// <returns>A view with the list of inactive condominiums.</returns>
        public async Task<IActionResult> InactiveCondominiums(int id)
        {
            var condominiums = await _repository.GetInactiveByCompanyIdAsync(id);

            ViewBag.CompanyId = id;

            return View(condominiums);
        }

        /// <summary>
        /// Reactivates a soft-deleted condominium.
        /// </summary>
        /// <param name="id">The ID of the condominium to activate.</param>
        /// <returns>A redirect to the inactive condominiums list.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var condominium = await _repository.GetByIdAsync(id);

            if (condominium != null)
            {
                condominium.IsActive = true;
                condominium.UserDeletedId = null; // Clear the deleted user ID
                condominium.DeletedAt = null; // Clear the deleted timestamp

                _repository.Update(condominium);
                await _repository.SaveAllAsync();

                TempData["StatusMessage"] = "Condominium reactivated successfully.";
            }

            return RedirectToAction(nameof(Index), new { id = condominium?.CompanyId });
        }
    }
}