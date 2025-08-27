using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class CondominiumsController : Controller
    {
        private readonly CondominiumDataContext _context;

        public CondominiumsController(CondominiumDataContext context)
        {
            _context = context;
        }

        // GET: Condominiums
        public async Task<IActionResult> Index(int id) // 'id' is the CompanyId
        {
            var condominiums = await _context.Condominiums.ToListAsync();
            ViewBag.CompanyId = id; // Pass the ID to the view
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
        public async Task<IActionResult> Create(CondominiumViewModel condominium)
        {
            // The `ModelState` will now correctly reflect validation errors for fields present in the form.
            // The `CompanyId` from the hidden field will be automatically bound.
            if (ModelState.IsValid)
            {
                var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var newCondominium = new Condominium

                {
                    CompanyId = condominium.CompanyId,
                    CondominiumManagerId = condominium.CondominiumManagerId,
                    Name = condominium.Name,
                    Address = condominium.Address,
                    PropertyRegistryNumber = condominium.PropertyRegistryNumber,
                    NumberOfUnits = condominium.NumberOfUnits,
                    ContractValue = condominium.ContractValue,
                    FeePerUnit = condominium.ContractValue / condominium.NumberOfUnits, // Calculate FeePerUnit
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserCreatedId = loggedInUserId
                };

                // 2. Add the Condominium to the database.
                _context.Add(newCondominium);
                await _context.SaveChangesAsync();

                // 3. Redirect back to the index page for the correct company.
                return RedirectToAction(nameof(Index), new { id = condominium.CompanyId });
            }

            // If ModelState is not valid, the form is re-displayed, and
            // asp-validation-summary="ModelOnly" will show the errors.
            return View(condominium);
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
            return View(condominium);
        }

        // POST: Condominiums/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyId,CondominiumManagerId,Name,Address,PropertyRegistryNumber,NumberOfUnits,ContractValue,FeePerUnit,CreatedAt,UpdatedAt,IsActive,UserCreatedId,UserUpdatedId,UserDeletedId")] Condominium condominium)
        {
            if (id != condominium.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(condominium);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CondominiumExists(condominium.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(condominium);
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

        // POST: Condominiums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var condominium = await _context.Condominiums.FindAsync(id);
            if (condominium != null)
            {
                _context.Condominiums.Remove(condominium);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CondominiumExists(int id)
        {
            return _context.Condominiums.Any(e => e.Id == id);
        }
    }
}
