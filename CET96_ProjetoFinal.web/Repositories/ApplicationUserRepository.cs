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
        /// <summary>
        /// Creates a new user with the specified password.
        /// </summary>
        /// <param name="user">The ApplicationUser entity to create.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>An IdentityResult indicating the success or failure of the operation.</returns>
        public async Task<IdentityResult> AddUserAsync(ApplicationUser user, string password) => await _userManager.CreateAsync(user, password);
        /// <summary>
        /// Assigns a user to a specific role.
        /// </summary>
        /// <param name="user">The user to add to the role.</param>
        /// <param name="roleName">The name of the role to add the user to.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task AddUserToRoleAsync(ApplicationUser user, string roleName) => await _userManager.AddToRoleAsync(user, roleName);
        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="user">The user whose password will be changed.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The new password for the user.</param>
        /// <returns>An IdentityResult indicating the success or failure of the password change.</returns>
        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string oldPassword, string newPassword) => await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        /// <summary>
        /// Confirms a user's email address.
        /// </summary>
        /// <param name="user">The user to confirm the email for.</param>
        /// <param name="token">The email confirmation token.</param>
        /// <returns>An IdentityResult indicating the success or failure of the email confirmation.</returns>
        public async Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token) => await _userManager.ConfirmEmailAsync(user, token);
        /// <summary>
        /// Generates a token for email confirmation.
        /// </summary>
        /// <param name="user">The user to generate the token for.</param>
        /// <returns>The generated email confirmation token.</returns>
        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user) => await _userManager.GenerateEmailConfirmationTokenAsync(user);
        /// <summary>
        /// Attempts to sign in the user with the specified username and password.
        /// </summary>
        /// <param name="model">The LoginViewModel containing the user's credentials.</param>
        /// <returns>A SignInResult indicating the result of the sign-in attempt.</returns>
        public async Task<SignInResult> LoginAsync(LoginViewModel model) => await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, true);
        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task LogoutAsync() => await _signInManager.SignOutAsync();
        /// <summary>
        /// Updates the user's details.
        /// </summary>
        /// <param name="user">The ApplicationUser entity to update.</param>
        /// <returns>An IdentityResult indicating the success or failure of the update.</returns>
        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user) => await _userManager.UpdateAsync(user);
        /// <summary>
        /// Sets a user's lockout end date.
        /// </summary>
        /// <param name="user">The user to set the lockout for.</param>
        /// <param name="lockoutEnd">The date and time the lockout will end.</param>
        /// <returns>An IdentityResult indicating the success or failure of the lockout operation.</returns>
        public async Task<IdentityResult> SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd) => await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        /// <summary>
        /// Checks if a user is in a specific role.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>True if the user is in the role; otherwise, false.</returns>
        public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName) => await _userManager.IsInRoleAsync(user, roleName);

        // --- Data Query Methods ---
        /// <summary>
        /// Finds a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>The ApplicationUser if found; otherwise, null.</returns>
        public async Task<ApplicationUser> GetUserByEmailasync(string email) => await _userManager.FindByEmailAsync(email);
        /// <summary>
        /// Finds a user by their user ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The ApplicationUser if found; otherwise, null.</returns>
        public async Task<ApplicationUser> GetUserByIdAsync(string userId) => await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        /// <summary>
        /// Gets a list of all users.
        /// </summary>
        /// <returns>A collection of all ApplicationUsers.</returns>
        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync() => await _context.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
        /// <summary>
        /// Gets a SelectList of users, typically for use in dropdown menus.
        /// </summary>
        /// <param name="selectedValue">The selected value for the list.</param>
        /// <returns>A SelectList of users.</returns>
        public async Task<SelectList> GetUsersForSelectListAsync(object? selectedValue = null)
        {
            var users = await _context.Users.OrderBy(u => u.UserName).ToListAsync();
            return new SelectList(users, "Id", "UserName", selectedValue);
        }
        /// <summary>
        /// Gets the roles assigned to a specific user.
        /// </summary>
        /// <param name="user">The user to retrieve the roles for.</param>
        /// <returns>A list of role names.</returns>
        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        //public async Task<List<ApplicationUser>> GetAllUsersByCompanyIdAsync(string userId)
        //{
        //    var user = await GetUserByIdAsync(userId);

        //    if (user == null)
        //    {
        //        return new List<ApplicationUser>();
        //    }

        //    var result = await _context.Users
        //        .Where(u => u.UserCreatedId != null && u.UserCreatedId == user.Id)
        //        .ToListAsync();

        //    return result;
        //}

        /// <summary>
        /// Gets all users belonging to a specific company.
        /// </summary>
        /// <param name="companyId">The ID of the company.</param>
        /// <returns>A collection of ApplicationUsers for that company.</returns>
        public async Task<IEnumerable<ApplicationUser>> GetUsersByCompanyIdAsync(int companyId)
        {
            return await _context.Users
                                 .Where(u => u.CompanyId == companyId)
                                 .OrderBy(u => u.FirstName)
                                 .ThenBy(u => u.LastName)
                                 .ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves all active staff members assigned to a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The unique identifier for the condominium.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable of ApplicationUser objects who are staff.
        /// </returns>
        public async Task<IEnumerable<ApplicationUser>> GetStaffByCondominiumIdAsync(int condominiumId)
        {
            return await _context.Users
                .Where(u => u.CondominiumId == condominiumId && u.DeactivatedAt == null)
                .ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves all active users associated with a specific company ID.
        /// An active user is one whose DeactivatedAt property is null.
        /// </summary>
        /// <param name="companyId">The unique identifier for the company.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable of ApplicationUser objects who are active.
        /// </returns>
        public async Task<IEnumerable<ApplicationUser>> GetActiveUsersByCompanyIdAsync(int companyId)
        {
            // Returns users from the specified company where DeactivatedAt is NULL (they are active)
            return await _context.Users
                .Where(u => u.CompanyId == companyId && u.DeactivatedAt == null)
                .ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves all inactive users associated with a specific company ID.
        /// An inactive user is one whose DeactivatedAt property has a value.
        /// </summary>
        /// <param name="companyId">The unique identifier for the company.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable of ApplicationUser objects who are inactive.
        /// </returns>
        public async Task<IEnumerable<ApplicationUser>> GetInactiveUsersByCompanyIdAsync(int companyId)
        {
            // Returns users from the specified company where DeactivatedAt has a value (they are inactive)
            return await _context.Users
                .Where(u => u.CompanyId == companyId && u.DeactivatedAt != null)
                .ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves all inactive users of a specific role for a given company.
        /// </summary>
        /// <param name="companyId">The unique identifier for the company.</param>
        /// <param name="roleName">The name of the role to filter by.</param>
        /// <returns>A collection of inactive ApplicationUser objects matching the criteria.</returns>
        public async Task<IEnumerable<ApplicationUser>> GetInactiveUsersByCompanyAndRoleAsync(int companyId, string roleName)
        {
            // 1. Get all users who are in the specified role.
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

            // 2. Filter that list to find users who belong to the specified company AND are inactive.
            var inactiveUsers = usersInRole.Where(u => u.CompanyId == companyId && u.DeactivatedAt.HasValue);

            return inactiveUsers.ToList();
        }
    }
}
