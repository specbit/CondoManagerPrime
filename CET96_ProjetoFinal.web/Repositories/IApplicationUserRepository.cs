using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Defines the contract for a single repository that handles all ApplicationUser
    /// data access and Identity-related business logic.
    /// </summary>
    public interface IApplicationUserRepository
    {
        // Methods for Identity operations (Lived in applicationUserHelper)
        Task<IdentityResult> AddUserAsync(ApplicationUser user, string password);
        Task AddUserToRoleAsync(ApplicationUser user, string roleName);
        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string oldPassword, string newPassword);
        Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token);
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
        Task<SignInResult> LoginAsync(LoginViewModel model);
        Task LogoutAsync();
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
        Task<IdentityResult> SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd);

        // Methods for data queries
        Task<ApplicationUser> GetUserByEmailasync(string email);
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<SelectList> GetUsersForSelectListAsync(object? selectedValue = null);
        Task<bool> IsInRoleAsync(ApplicationUser user, string roleName);

        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    }
}
