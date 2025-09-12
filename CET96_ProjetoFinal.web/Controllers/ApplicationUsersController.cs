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

        public async Task<IActionResult> AllUsers()
        {
            var allUsers = await _applicationUserHelper.GetAllUsersAsync();
                
            var model = new List<ApplicationUserViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _applicationUserHelper.GetUserRolesAsync(user);
                model.Add(new ApplicationUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    Roles = roles
                });
            }

            return View(model);
        }
    }
}
