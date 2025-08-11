using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class ApplicationUsersController : Controller
    {
        private readonly IApplicationUserHelper _applicationUserHelper;

        public ApplicationUsersController(IApplicationUserHelper applicationUserHelper)
        {
            _applicationUserHelper = applicationUserHelper;
        }

        // This action will be at /ApplicationUsers/AllUsers
        public async Task<IActionResult> AllUsers()
        {
            // First, add the necessary methods to your IApplicationUserHelper interface
            // and implement them in ApplicationUserHelper.cs:
            // - Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
            // - Task<IList<string>> GetUserRolesAsync(ApplicationUser user)

            var allUsers = await _applicationUserHelper.GetAllUsersAsync();
            var model = new List<ApplicationUserWithRolesViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _applicationUserHelper.GetUserRolesAsync(user);
                model.Add(new ApplicationUserWithRolesViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    Roles = string.Join(", ", roles) // Join roles into a single string
                });
            }

            return View(model);
        }
    }
}
