using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class CondominiumsController : Controller
    {
        private readonly CondominiumDataContext _context;

        public CondominiumsController(CondominiumDataContext context)
        {
            _context = context;
        }

        // GET: Condominiums for a specific Company
        public async Task<IActionResult> Index(int id) // 'id' is the CompanyId
        {
            // Fetch only the condominiums for the given company
            var condominiums = await _context.Condominiums
                .Where(c => c.CompanyId == id && c.IsActive == true)
                .ToListAsync();

            ViewBag.CompanyId = id;

            return View(condominiums);
        }

        // GET: Condominiums/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var condominium = await _context.Condominiums
                .FirstOrDefaultAsync(m => m.Id == id);
            if (condominium == null)
            {
                return NotFound();
            }

            return View(condominium);
        }

        // GET: Condominiums/Create
        public IActionResult Create(int id) // 'id' will be the CompanyId
        {
            // You can pass the CompanyId to the view using ViewData or a ViewModel
            ViewData["CompanyId"] = id;
            return View();
        }

        // POST: Condominiums/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CondominiumViewModel model)
        {
            // Check if the address is already in use for this company.
            bool addressExists = await _context.Condominiums
                .AnyAsync(c => c.Address == model.Address && c.CompanyId == model.CompanyId);
            if (addressExists)
            {
                ModelState.AddModelError("Address", "This address is already registered for another condominium in your company.");
            }

            // Check if the Property Registry Number is already in use for this company.
            bool registryNumberExists = await _context.Condominiums
                .AnyAsync(c => c.PropertyRegistryNumber == model.PropertyRegistryNumber && c.CompanyId == model.CompanyId);
            if (registryNumberExists)
            {
                ModelState.AddModelError("PropertyRegistryNumber", "This Property Registry Number is already in use.");
            }

            if (ModelState.IsValid)
            {
                // Server-side check for division by zero
                if (model.NumberOfUnits <= 0)
                {
                    ModelState.AddModelError("NumberOfUnits", "Number of Units must be greater than zero.");
                    return View(model);
                }

                var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var newCondominium = new Condominium
                {
                    CompanyId = model.CompanyId,
                    Name = model.Name,
                    Address = model.Address,
                    PropertyRegistryNumber = model.PropertyRegistryNumber,
                    NumberOfUnits = model.NumberOfUnits,
                    ContractValue = model.ContractValue,
                    // Secure server-side calculation
                    FeePerUnit = model.ContractValue / model.NumberOfUnits,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserCreatedId = loggedInUserId
                };

                _context.Add(newCondominium);
                await _context.SaveChangesAsync();

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

            var condominium = await _context.Condominiums.FindAsync(id);
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
                PropertyRegistryNumber = condominium.PropertyRegistryNumber,
                NumberOfUnits = condominium.NumberOfUnits,
                ContractValue = condominium.ContractValue,
                FeePerUnit = condominium.FeePerUnit
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
            bool addressExists = await _context.Condominiums
                .AnyAsync(c => c.Address == model.Address && c.CompanyId == model.CompanyId && c.Id != model.Id); // Exclude self
            if (addressExists)
            {
                ModelState.AddModelError("Address", "This address is already registered for another condominium in your company.");
            }

            // Check if registry number exists for another condominium in this company.
            bool registryNumberExists = await _context.Condominiums
                .AnyAsync(c => c.PropertyRegistryNumber == model.PropertyRegistryNumber && c.CompanyId == model.CompanyId && c.Id != model.Id); // Exclude self
            if (registryNumberExists)
            {
                ModelState.AddModelError("PropertyRegistryNumber", "This Property Registry Number is already in use.");
            }

            if (ModelState.IsValid)
            {
                // Server-side check for division by zero
                if (model.NumberOfUnits <= 0)
                {
                    ModelState.AddModelError("NumberOfUnits", "Number of Units must be greater than zero.");
                    return View(model);
                }

                // 1. Fetch the original condominium from the database.
                var condominiumToUpdate = await _context.Condominiums.FindAsync(id);
                if (condominiumToUpdate == null) return NotFound();

                // 2. Manually update the properties. This prevents over-posting.
                condominiumToUpdate.Name = model.Name;
                condominiumToUpdate.Address = model.Address;
                condominiumToUpdate.PropertyRegistryNumber = model.PropertyRegistryNumber;
                condominiumToUpdate.NumberOfUnits = model.NumberOfUnits;
                condominiumToUpdate.ContractValue = model.ContractValue;

                // 3. Perform the secure server-side calculation.
                condominiumToUpdate.FeePerUnit = model.ContractValue / model.NumberOfUnits;

                // 4. Update audit fields.
                condominiumToUpdate.UpdatedAt = DateTime.UtcNow;
                condominiumToUpdate.UserUpdatedId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                try
                {
                    _context.Update(condominiumToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CondominiumExists(model.Id)) return NotFound();
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

            var condominium = await _context.Condominiums
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var condominium = await _context.Condominiums.FindAsync(id);
            if (condominium != null)
            {
                // 1. Set the flag to inactive instead of removing the record.
                condominium.IsActive = false;

                // 2. Set the audit fields for the soft delete.
                condominium.UserDeletedId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // You might also want a 'DeletedAt' field in your entity.

                _context.Update(condominium);
                await _context.SaveChangesAsync();
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
            var condominiums = await _context.Condominiums
                .Where(c => c.CompanyId == id && !c.IsActive)
                .ToListAsync();

            ViewBag.CompanyId = id;
            return View(condominiums); // You will need to create this new view.
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
            var condominium = await _context.Condominiums.FindAsync(id);
            if (condominium != null)
            {
                condominium.IsActive = true;
                condominium.UserDeletedId = null; // Clear the deleted user ID

                _context.Update(condominium);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Condominium reactivated successfully.";
            }

            return RedirectToAction(nameof(InactiveCondominiums), new { id = condominium?.CompanyId });
        }

        private bool CondominiumExists(int id)
        {
            return _context.Condominiums.Any(e => e.Id == id);
        }
    }
}
