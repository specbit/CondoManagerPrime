using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IApplicationUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ICondominiumRepository _condominiumRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IApplicationUserRepository userRepository,
            ICompanyRepository companyRepository,
            ICondominiumRepository condominiumRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _condominiumRepository = condominiumRepository;
        }

        /// <summary>
        /// Serves the main home page (dashboard), which displays dynamic content based on the 
        /// logged-in user's role.
        /// </summary>
        /// <remarks>
        /// If the user is not authenticated, a generic public welcome page is shown. 
        /// For authenticated users, the content varies:
        /// - Company Administrators will see a list of their managed companies.
        /// - Condominium Managers will see details of their single assigned condominium.
        /// - Other roles (like Platform Administrator) will see a generic logged-in page.
        /// The method populates and returns a HomeViewModel tailored to the user's context.
        /// </remarks>
        /// <returns>
        /// A Task<IActionResult> that renders the home page view, populated with a 
        /// HomeViewModel containing role-specific data.
        /// </returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel();

            // First, check if a user is logged in at all.
            if (User.Identity.IsAuthenticated)
            {
                // Fetch the user object once, as it's needed for multiple roles.
                var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);

                if (user != null)
                {
                    if (User.IsInRole("Company Administrator"))
                    {
                        model.Companies = await _companyRepository.GetCompaniesByUserIdAsync(user.Id);
                    }
                    else if (User.IsInRole("Condominium Manager"))
                    {
                        // --- LOGIC to load condominium data ---
                        // Now 'user' is available.
                        var condominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(user.Id);

                        if (condominium != null)
                        {
                            model.IsManagerAssignedToCondominium = true;
                            model.CondominiumId = condominium.Id;
                            model.CondominiumName = condominium.Name;
                            model.CondominiumAddress = condominium.Address;
                            model.City = condominium.City;
                            model.ZipCode = condominium.ZipCode;
                            model.UnitsCount = condominium.Units.Count(); // <-- CALCULATE THE COUNT
                            model.CompanyId = user.CompanyId ?? 0;

                            // Fetch the staff for this condominium.
                            var staff = await _userRepository.GetStaffByCondominiumIdAsync(condominium.Id);
                            // Convert the IEnumerable to a List safely using .ToList()
                            model.CondominiumStaff = staff.ToList();
                        }
                    }
                }
            }

            // If the user is not authenticated, or is in a role without a custom dashboard (like Platform Admin),
            // they will get the default view determined by the Index.cshtml logic.
            return View(model);
        }

        /// <summary>
        /// Asynchronously prepares and returns the dashboard view for administrative roles.
        /// </summary>
        /// <remarks>
        /// This action is restricted to authenticated users. It dynamically populates the 
        /// <see cref="HomeViewModel"/> based on the user's role:
        /// - If the user is a 'Platform Administrator', it loads a list of all users in the system.
        /// - If the user is a 'Company Administrator', it loads the list of companies associated with them.
        /// For other authenticated users, it returns the view with an empty model.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{IActionResult}"/> that renders the dashboard view, populated with a 
        /// <see cref="HomeViewModel"/> containing data specific to the administrator's role.
        /// </returns>
        [Authorize]
        public async Task<IActionResult> HomePlatformAdmin()
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

        //[Authorize(Roles = "Condominium Manager")]
        //public async Task<IActionResult> CondominiumManagerDashboard()
        //{
        //    var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
        //    if (loggedInUser == null)
        //    {
        //        // This should not happen for an authenticated user, but is a good safeguard.
        //        return Unauthorized();
        //    }

        //    var managedCondominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);

        //    if (managedCondominium == null)
        //    {
        //        // Handle case where a manager is not assigned to a condominium.
        //        return View("NoCondominiumAssigned");
        //    }

        //    // Now, fetch the staff for that condominium.
        //    var staffList = await _userRepository.GetStaffByCondominiumIdAsync(managedCondominium.Id);

        //    var viewModel = new CondominiumManagerDashboardViewModel
        //    {
        //        Condominium = managedCondominium,
        //        Staff = staffList
        //    };

        //    return View(viewModel);
        //}

        [Authorize(Roles = "Condominium Manager")]
        [HttpGet]
        public IActionResult CondominiumManagerDashboard()
        {
            return RedirectToAction(nameof(Index));
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

        /// <summary>
        /// Builds the manager dashboard portion of the home page:
        /// loads the manager's assigned condominium and its staff.
        /// </summary>
        /// <param name="manager">The authenticated user in the 'Condominium Manager' role.</param>
        /// <returns>A HomeViewModel prefilled with manager-specific data.</returns>
        private async Task<HomeViewModel> BuildManagerDashboardAsync(ApplicationUser manager)
        {
            // 1:1 rule – each manager manages exactly one condo (or none yet).
            var condo = await _condominiumRepository.GetCondominiumByManagerIdAsync(manager.Id);

            // If not assigned, keep staff empty and let the view show a friendly message.
            var staff = condo is null
                ? new List<ApplicationUser>()
                : (await _userRepository.GetStaffByCondominiumIdAsync(condo.Id)).ToList();

            // Map to your existing HomeViewModel used by Home/Index.
            return new HomeViewModel
            {
                CompanyName = manager.CompanyName,     // adjust if your user entity exposes this differently
                IsManagerAssignedToCondominium = condo != null,
                CondominiumName = condo?.Name,
                CondominiumAddress = condo?.Address,
                ZipCode = condo?.ZipCode,
                City = condo?.City,
                UnitsCount = condo?.Units?.Count ?? 0,
                CondominiumId = condo?.Id ?? 0,
                CondominiumStaff = staff
            };
        }

    }
}