using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for .Include()
using System.Diagnostics;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class HomeController : Controller
    {
        // Just DbContext for this more direct query (for Admin user dashboard).
        private readonly ApplicationUserDataContext _context;

        public HomeController(ApplicationUserDataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Check if a user is logged in
            if (User.Identity.IsAuthenticated)
            {
                // Fetch the user from the database and explicitly include their Company data in one query
                var user = await _context.Users
                    .Include(u => u.Company) // This is the key line that joins the tables
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                // Check if the user was found and has a company linked
                if (user != null && user.Company != null)
                {
                    var model = new HomeViewModel
                    {
                        CompanyName = user.Company.Name
                    };
                    return View(model);
                }
            }

            // If the user is not logged in or has no company, show the default view
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