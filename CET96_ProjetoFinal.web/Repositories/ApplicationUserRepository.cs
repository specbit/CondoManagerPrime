using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Implements the repository for handling all ApplicationUser data and identity operations.
    /// </summary>
    public class ApplicationUserRepository : IApplicationUserRepository
    {
        private readonly ApplicationUserDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // The constructor now correctly injects all the services we need.
        public ApplicationUserRepository(
            ApplicationUserDataContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // --- Identity Methods ---
        public async Task<IdentityResult> AddUserAsync(ApplicationUser user, string password) => await _userManager.CreateAsync(user, password);
        public async Task AddUserToRoleAsync(ApplicationUser user, string roleName) => await _userManager.AddToRoleAsync(user, roleName);
        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string oldPassword, string newPassword) => await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        public async Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token) => await _userManager.ConfirmEmailAsync(user, token);
        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user) => await _userManager.GenerateEmailConfirmationTokenAsync(user);
        public async Task<SignInResult> LoginAsync(LoginViewModel model) => await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);
        public async Task LogoutAsync() => await _signInManager.SignOutAsync();
        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user) => await _userManager.UpdateAsync(user);
        public async Task<IdentityResult> SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd) => await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName) => await _userManager.IsInRoleAsync(user, roleName);

        // --- Data Query Methods ---
        public async Task<ApplicationUser> GetUserByEmailasync(string email) => await _userManager.FindByEmailAsync(email);
        public async Task<ApplicationUser> GetUserByIdAsync(string userId) => await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync() => await _context.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();

        public async Task<SelectList> GetUsersForSelectListAsync(object? selectedValue = null)
        {
            var users = await _context.Users.OrderBy(u => u.UserName).ToListAsync();
            return new SelectList(users, "Id", "UserName", selectedValue);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<List<ApplicationUser>> GetAllUsersByCompanyIdAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);

            if (user == null)
            {
                return new List<ApplicationUser>();
            }

            var result = await _context.Users
                .Where(u => u.UserCreatedId != null && u.UserCreatedId == user.Id)
                .ToListAsync();

            return result;
        }
    }
}
