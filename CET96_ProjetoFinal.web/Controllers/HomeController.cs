using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for .Include()
using System.Diagnostics;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class HomeController : Controller
    {
        // Just DbContext for this more direct query (for Admin user dashboard).
        private readonly ApplicationUserDataContext _context;
        private readonly ICompanyRepository _companiesRepository;
        private readonly IApplicationUserHelper _userHelper;

        public HomeController(
            ApplicationUserDataContext context, 
            ICompanyRepository companiesRepository, 
            IApplicationUserHelper userHelper)
        {
            _context = context;
            _companiesRepository = companiesRepository;
            _userHelper = userHelper;
        }

        //public async Task<IActionResult> Index()
        //{
        //    // Check if a user is logged in
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        // Fetch the user from the database and explicitly include their Company data in one query
        //        var user = await _context.Users
        //            .Include(u => u.Company) // This is the key line that joins the tables
        //            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        //        // Check if the user was found and has a company linked
        //        if (user != null && user.Company != null)
        //        {
        //            var model = new HomeViewModel
        //            {
        //                CompanyName = user.Company.Name
        //            };
        //            return View(model);
        //        }
        //    }

        //    // If the user is not logged in or has no company, show the default view
        //    return View();
        //}

        public async Task<IActionResult> Index()
        {
            // Check if a user is logged in and has the correct administrator role.
            if (User.Identity.IsAuthenticated && User.IsInRole("Company Administrator"))
            {
                // First, get the full user object for the person who is logged in.
                var user = await _userHelper.GetUserByEmailasync(User.Identity.Name);
                if (user == null)
                {
                    // This is a safety check; it should not happen for a logged-in user.
                    return NotFound();
                }

                // Next, use the repository to fetch the list of companies created by this specific user.
                var companies = await _companiesRepository.GetCompaniesByUserIdAsync(user.Id);

                // We create our ViewModel and pass the list of companies to it.
                var model = new HomeViewModel
                {
                    Companies = companies
                };

                // Finally, we pass the model (containing the list) to the View.
                return View(model);
            }

            // If the user is not logged in or not an admin, show the default public view.
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}