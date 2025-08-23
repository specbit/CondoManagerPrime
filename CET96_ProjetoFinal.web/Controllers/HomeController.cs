using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class HomeController : Controller
    {
        // Just DbContext for this more direct query (for Admin user dashboard).
        private readonly ICompanyRepository _companyRepository;
        private readonly IApplicationUserRepository _userRepository;

        // The constructor is updated to ask for the new IApplicationUserRepository
        public HomeController(ICompanyRepository companyRepository, IApplicationUserRepository userRepository)
        {
            _companyRepository = companyRepository;
            _userRepository = userRepository;
        }

        //public async Task<IActionResult> Index()
        //{
        //    // Check if a user is logged in and has the correct administrator role.
        //    if (User.Identity.IsAuthenticated && User.IsInRole("Company Administrator"))
        //    {
        //        // First, get the full user object for the person who is logged in.
        //        var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);
        //        if (user == null)
        //        {
        //            // This is a safety check; it should not happen for a logged-in user.
        //            return NotFound();
        //        }

        //        // Next, use the repository to fetch the list of companies created by this specific user.
        //        var companies = await _companyRepository.GetCompaniesByUserIdAsync(user.Id);

        //        // We create our ViewModel and pass the list of companies to it.
        //        var model = new HomeViewModel
        //        {
        //            Companies = companies
        //        };

        //        // Finally, we pass the model (containing the list) to the View.
        //        return View(model);
        //    }

        //    // If the user is not logged in or not an admin, show the default public view.
        //    return View();
        //}
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel(); // Create an empty ViewModel first

            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Platform Administrator"))
                {
                    // If Platform Admin, get all users and add them to the model
                    var allUsers = await _userRepository.GetAllUsersAsync();

                    var userViewModelList = new List<ApplicationUserViewModel>();

                    foreach (var user in allUsers)
                    {
                        var roles = await _userRepository.GetUserRolesAsync(user);

                        userViewModelList.Add(new ApplicationUserViewModel
                        {
                            Id = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            UserName = user.UserName, // Or user.Email
                            IsDeactivated = user.DeactivatedAt.HasValue,
                            Roles = roles
                        });
                    }
                    model.AllUsers = userViewModelList;
                }
                else if (User.IsInRole("Company Administrator"))
                {
                    // If Company Admin, get their companies and add them to the model
                    var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);
                    if (user != null)
                    {
                        model.Companies = await _companyRepository.GetCompaniesByUserIdAsync(user.Id);
                    }
                }
            }

            // Pass the model (which may or may not be populated) to the single Index view
            return View(model);
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