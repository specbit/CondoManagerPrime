using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator")]
    public class CompaniesController : Controller
    {
        private readonly IApplicationUserHelper _userHelper;

        public CompaniesController(IApplicationUserHelper userHelper)
        {
            _userHelper = userHelper;
        }

        // GET: /Companies/Create
        public async Task<IActionResult> Create()
        {
            // Get the currently logged-in user
            var user = await _userHelper.GetUserByEmailasync(User.Identity.Name);
            if (user == null)
            {
                // This should not happen if the user is logged in
                return NotFound();
            }

            // Create the ViewModel and pre-fill the Name property
            var model = new CompanyViewModel
            {
                Name = user.CompanyName
            };

            // Pass the pre-filled model to the view
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
                    taxId = model.TaxId
                });
            }
            return View(model);
        }
    }
}
