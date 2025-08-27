using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator")]
    /// <summary>
    /// Handles the payment simulation and final creation of a new company.
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly ApplicationUserDataContext _context;
        IApplicationUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly ICompanyRepository _companiesRepository;

        // The constructor must request the services that are registered in Program.cs
        public PaymentController(
            ApplicationUserDataContext context,
            IApplicationUserRepository userRepository, 
            IEmailSender emailSender,
            ICompanyRepository companiesRepository)
        {
            _context = context;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _companiesRepository = companiesRepository;
        }

        // GET: /Payment/Create
        [HttpGet]
        public IActionResult Create(string name, string description, string taxId, string address, string phoneNumber, string email)
        {
            var model = new PaymentViewModel
            {
                CompanyName = name,
                CompanyDescription = description,
                CompanyTaxId = taxId,
                CompanyAddress = address,
                CompanyPhoneNumber = phoneNumber,
                CompanyEmail = email
            };

            return View(model);
        }

        // POST: /Payment/Create
        /// <summary>
        /// Finalizes the company creation process after payment simulation. This action
        /// performs server-side validation to prevent duplicate data, creates the new Company entity,
        /// links it to the current user, saves all changes to the database in a single transaction,
        /// and sends a welcome email.
        /// </summary>
        /// <param name="model">The payment view model containing the company's details.</param>
        /// <returns>A redirect to the administrator's dashboard on success, or back to the
        /// form with validation errors on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Get the user from the database.
            var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);

            if (user == null)
            {
                return NotFound();
            }


            // 2. Create the new Company object in memory.
            var company = new Company
            {
                Name = model.CompanyName,
                Description = model.CompanyDescription,
                TaxId = model.CompanyTaxId,
                Address = model.CompanyAddress,
                PhoneNumber = model.CompanyPhoneNumber,
                Email = model.CompanyEmail,
                ApplicationUserId = user.Id,
                UserCreatedId = user.Id,
                PaymentValidated = true // Set to true to indicate payment has been validated
            };

            // 3. Add the new company to the DbContext.
            _context.Companies.Add(company);

            // 4. CRUCIAL STEP: Update the user object that is already being tracked.
            // We do NOT need to call UpdateUserAsync. We just modify the object.
            // By setting the navigation property directly, we are telling Entity Framework:
            // "This user object is now related to this company object."
            // Instead of setting the CompanyId directly, we set the navigation property.
            // user.CompanyId = company.Id; // Set the CompanyId in the user
            user.Company = company; // Link the company to the user

            // 5. Save everything in a single transaction.
            // Entity Framework is smart enough to see a new Company and an updated User
            // and will save both correctly.
            await _context.SaveChangesAsync();

            // 6. Send a welcome email to the company's contact email 
            try
            {
                await _emailSender.SendEmailAsync(
                    company.Email,
                    $"Welcome to CondoManagerPrime, {company.Name}!",
                    $"<h1>Welcome!</h1><p>Your company, {company.Name}, has been successfully registered on the CondoManagerPrime platform.</p>"
                );
            }
            catch (Exception)
            {
                // Optional: Log exception. Don't show an error to the user
                // because the company creation was successful.
            }

            TempData["StatusMessage"] = "Company created and payment confirmed successfully!";
            
            return RedirectToAction("Index", "Home");
        }
    }
}