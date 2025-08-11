using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Identity;

namespace CET96_ProjetoFinal.web.Helpers
{
    public interface IApplicationUserHelper
    {
        Task<ApplicationUser> GetUserByEmailasync(string email);
        Task<IdentityResult> AddUserAsync(ApplicationUser user, string password);
        Task CheckRoleAsync(string roleName);
        Task AddUserToRoleAsync(ApplicationUser user, string roleName);
        Task<SignInResult> LoginAsync(LoginViewModel model);
        Task LogoutAsync();

        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    }
}
