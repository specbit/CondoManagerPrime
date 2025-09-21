using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator")]
    public class CompaniesController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly ICompanyRepository _companiesRepository; 

        public CompaniesController(IApplicationUserRepository userHelper, ICompanyRepository companiesRepository)
        {
            _userRepository = userHelper;
            _companiesRepository = companiesRepository;
        }

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
        /// <summary>
        /// Prepares and displays the company creation form. The form will be
        /// pre-filled with a company name if one is provided via the companyName parameter,
        /// otherwise it will be blank for creating a new company.
        /// </summary>
        /// <param name="companyName">An optional company name, typically passed from the initial user
        /// registration flow, to pre-fill the form.</param>
        /// <returns>A ViewResult displaying the Create.cshtml page with a CompanyViewModel.</returns>
        public IActionResult Create(string? companyName)
        {
            var model = new CompanyViewModel();

            if (!string.IsNullOrEmpty(companyName))
            {
                // This block runs ONLY when the name is passed from the registration flow.
                model.Name = companyName;
            }

            // If no name is passed (from the dashboard button), an empty model is returned.
            return View(model);
        }

        // POST: /Companies/Create
        /// <summary>
        /// Handles the submission of the new company form. It performs comprehensive
        /// server-side validation for both data annotations and for duplicate data (Name, Tax ID, etc.)
        /// before redirecting to the payment step.
        /// </summary>
        /// <param name="model">The view model containing the data for the new company submitted from the form.</param>
        /// <returns>A RedirectToAction to the payment controller on successful validation, or the Create
        /// view with error messages if any validation fails.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyViewModel model)
        {
            // First, check the basic data annotations (like [Required], [EmailAddress])
            if (ModelState.IsValid)
            {
                // Check if the Name is already in use
                if (await _companiesRepository.IsNameInUseAsync(model.Name))
                {
                    ModelState.AddModelError("Name", "This Company Name is already registered.");
                }

                // Check if the Tax ID is already in use
                if (await _companiesRepository.IsTaxIdInUseAsync(model.TaxId))
                {
                    ModelState.AddModelError("TaxId", "This Tax ID is already registered.");
                }

                // Check if the Email is already in use
                if (await _companiesRepository.IsEmailInUseAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already in use by another company.");
                }

                // Check if the Phone Number is already in use
                if (await _companiesRepository.IsPhoneNumberInUseAsync(model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "This Phone Number is already in use by another company.");
                }

                // If all validations pass (both basic and custom), proceed to payment
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
            }

            // If any validation fails, return to the form to display the errors.
            return View(model);
        }

        // UPDATE
        // GET: Companies/Edit/5
        /// <summary>
        /// Prepares and displays the form for editing a company.
        /// </summary>
        /// <param name="id">The ID of the company to edit.</param>
        /// <param name="source">An optional string to enable a context-aware "Back" link.</param>
        /// <returns>The Edit view with the company's data.</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, string? source)
        {
            if (id == null) return NotFound();

            // Fetch the company from the repository
            var company = await _companiesRepository.GetByIdAsync(id.Value);

            // Check if the company exists
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

            ViewData["Source"] = source; // Pass the source to the view for context-aware navigation

            return View(model);
        }

        // POST: Companies/Edit/5
        /// <summary>
        /// Handles the submission of the company edit form. It validates the data,
        /// updates the company record, and redirects the user back to their original
        /// location (either the dashboard or the inactive list).
        /// </summary>
        /// <param name="id">The ID of the company being edited, from the URL.</param>
        /// <param name="model">The view model containing the updated company data from the form.</param>
        /// <param name="source">An optional string indicating the originating page (e.g., "inactive")
        /// to enable a context-aware redirect after saving.</param>
        /// <returns>A RedirectToAction on success, or the Edit view with validation errors on failure.</returns>        [HttpPost]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CompanyViewModel model, string? source)
        {
            if (id != model.Id) return NotFound();

            // Note: We pass model.Id to exclude the current company from the check
            if (await _companiesRepository.IsNameInUseAsync(model.Name, model.Id))
            {
                ModelState.AddModelError("Name", "This Company Name is already registered.");
            }
            if (await _companiesRepository.IsTaxIdInUseAsync(model.TaxId, model.Id))
            {
                ModelState.AddModelError("TaxId", "This Tax ID is already registered.");
            }
            if (await _companiesRepository.IsEmailInUseAsync(model.Email, model.Id))
            {
                ModelState.AddModelError("Email", "This email is already in use by another company.");
            }
            if (await _companiesRepository.IsPhoneNumberInUseAsync(model.PhoneNumber, model.Id))
            {
                ModelState.AddModelError("PhoneNumber", "This Phone Number is already in use by another company.");
            }

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
                catch (DbUpdateConcurrencyException) // Handle concurrency issues
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

                // Redirect based on the source parameter
                if (source == "inactive")
                {
                    return RedirectToAction(nameof(InactiveCompanies));
                }

                else
                {
                    return RedirectToAction("Index", "Home"); // Redirect to the main dashboard after editing
                }
            }

            ViewData["Source"] = source; // Pass the source to the view
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
        /// <summary>
        /// Handles the POST request to deactivate a company (soft delete). It sets the
        /// company's IsActive flag to false and records the user and timestamp of the action.
        /// </summary>
        /// <param name="id">The ID of the company to deactivate.</param>
        /// <returns>A RedirectToAction to the home dashboard.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Fetch the company from the repository
            var company = await _companiesRepository.GetByIdAsync(id);

            if (company != null)
            {
                // Get the user who is performing the action
                var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);

                company.IsActive = false;  // Soft delete by marking as inactive
                company.DeletedAt = DateTime.UtcNow; // Record WHEN it was deactivated
                company.UserDeletedId = user.Id;     // Record WHO deactivated it
                
                _companiesRepository.Update(company); // Mark the entity as modified
                await _companiesRepository.SaveAllAsync(); // Save changes to the database
            }
            return RedirectToAction("Index", "Home");
        }
        // GET: Companies/InactiveCompanies
        /// <summary>
        /// Displays a view listing all companies that have been marked as inactive
        /// for the current administrator.
        /// </summary>
        /// <returns>A ViewResult with a list of inactive companies.</returns>
        public async Task<IActionResult> InactiveCompanies()
        {
            var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var inactiveCompanies = await _companiesRepository.GetInactiveCompaniesByUserIdAsync(user.Id);
            return View(inactiveCompanies);
        }

        // GET: Companies/Reactivate/5
        /// <summary>
        /// Displays a confirmation page before reactivating a company.
        /// </summary>
        /// <param name="id">The ID of the company to be reactivated.</param>
        /// <returns>A ViewResult with the company's details.</returns>
        public async Task<IActionResult> Reactivate(int? id)
        {
            if (id == null) return NotFound();
            var company = await _companiesRepository.GetByIdAsync(id.Value);
            if (company == null) return NotFound();
            return View(company);
        }

        // POST: Companies/Reactivate/5
        /// <summary>
        /// Handles the POST request to reactivate a company. It sets the IsActive flag to true
        /// and clears the deactivation audit fields.
        /// </summary>
        /// <param name="id">The ID of the company to reactivate.</param>
        /// <returns>A RedirectToAction to the main dashboard on success.</returns>
        [HttpPost, ActionName("Reactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateConfirmed(int id)
        {
            var company = await _companiesRepository.GetByIdAsync(id);

            if (company != null)
            {
                company.IsActive = true;
                company.DeletedAt = null;     // Clear the deactivation date
                company.UserDeletedId = null; // Clear the deactivation user

                _companiesRepository.Update(company); // Mark the entity as modified

                await _companiesRepository.SaveAllAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}