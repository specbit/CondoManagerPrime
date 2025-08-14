using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator")]
    public class PaymentController : Controller
    {
        private readonly ApplicationUserDataContext _context;
        private readonly IApplicationUserHelper _userHelper;

        // The constructor must request the services that are registered in Program.cs
        public PaymentController(ApplicationUserDataContext context, IApplicationUserHelper userHelper)
        {
            _context = context;
            _userHelper = userHelper;
        }

        // GET: /Payment/Create
        [HttpGet]
        public IActionResult Create(string name, string description, string taxId)
        {
            var model = new PaymentViewModel
            {
                CompanyName = name,
                CompanyDescription = description,
                CompanyTaxId = taxId
            };
            return View(model);
        }

        // POST: /Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userHelper.GetUserByEmailasync(User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }

            var company = new Company
            {
                Name = model.CompanyName,
                Description = model.CompanyDescription,
                TaxId = model.CompanyTaxId,
                ApplicationUserId = user.Id,
                UserCreatedId = user.Id,
                PaymentValidated = true // Payment is successful
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Company created and payment confirmed successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
}