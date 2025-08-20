using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories; // Add this using directive
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator")]
    public class CompaniesController : Controller
    {
        private readonly IApplicationUserHelper _userHelper;
        private readonly ICompaniesRepository _companiesRepository; // Inject the repository

        // Update the constructor to accept the repository
        public CompaniesController(IApplicationUserHelper userHelper, ICompaniesRepository companiesRepository)
        {
            _userHelper = userHelper;
            _companiesRepository = companiesRepository;
        }

        // READ (List)
        // GET: Companies
        public async Task<IActionResult> Index()
        {
            var companies = await _companiesRepository.GetAllWithCreatorsAsync();
            return View(companies);
        }

        // READ (Single Item)
        // GET: Companies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var company = await _companiesRepository.GetByIdWithCreatorAsync(id.Value);
            if (company == null) return NotFound();

            return View(company);
        }

        // GET: /Companies/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userHelper.GetUserByEmailasync(User.Identity.Name);
            if (user == null) return NotFound();
            var model = new CompanyViewModel { Name = user.CompanyName };
            return View(model);
        }

        // POST: /Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CompanyViewModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Create", "Payment", new
                {
                    name = model.Name,
                    description = model.Description,
                    taxId = model.TaxId,
                    address = model.Address,
                    phoneNumber = model.PhoneNumber,
                    email = model.Email
                });
            }
            return View(model);
        }

        // UPDATE
        // GET: Companies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var company = await _companiesRepository.GetByIdAsync(id.Value);
            if (company == null) return NotFound();

            // Map the database entity to the ViewModel for the view
            var model = new CompanyViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Description = company.Description,
                TaxId = company.TaxId,
                Address = company.Address,
                PhoneNumber = company.PhoneNumber,
                Email = company.Email
            };

            return View(model);
        }

        // POST: Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CompanyViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original entity from the database
                    var company = await _companiesRepository.GetByIdAsync(id);
                    if (company == null) return NotFound();

                    // Update its properties from the ViewModel
                    company.Name = model.Name;
                    company.Description = model.Description;
                    company.TaxId = model.TaxId;
                    company.Address = model.Address;
                    company.PhoneNumber = model.PhoneNumber;
                    company.Email = model.Email;

                    _companiesRepository.Update(company);
                    await _companiesRepository.SaveAllAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _companiesRepository.ExistsAsync(model.Id))
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
            return View(model);
        }

        // DELETE
        // GET: Companies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var company = await _companiesRepository.GetByIdWithCreatorAsync(id.Value);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Companies/Delete/5 (This performs the Deactivate action)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _companiesRepository.GetByIdAsync(id);
            if (company != null)
            {
                company.IsActive = false; // Soft delete
                _companiesRepository.Update(company);
                await _companiesRepository.SaveAllAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}