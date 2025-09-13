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
        // --- Identity Methods ---
        /// <summary>
        /// Creates a new user with the specified password.
        /// </summary>
        /// <param name="user">The ApplicationUser entity to create.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>An IdentityResult indicating the success or failure of the operation.</returns>
        Task<IdentityResult> AddUserAsync(ApplicationUser user, string password);
        /// <summary>
        /// Assigns a user to a specific role.
        /// </summary>
        /// <param name="user">The user to add to the role.</param>
        /// <param name="roleName">The name of the role to add the user to.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task AddUserToRoleAsync(ApplicationUser user, string roleName);

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="user">The user whose password will be changed.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The new password for the user.</param>
        /// <returns>An IdentityResult indicating the success or failure of the password change.</returns>
        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string oldPassword, string newPassword);

        /// <summary>
        /// Confirms a user's email address.
        /// </summary>
        /// <param name="user">The user to confirm the email for.</param>
        /// <param name="token">The email confirmation token.</param>
        /// <returns>An IdentityResult indicating the success or failure of the email confirmation.</returns>
        Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token);

        /// <summary>
        /// Generates a token for email confirmation.
        /// </summary>
        /// <param name="user">The user to generate the token for.</param>
        /// <returns>The generated email confirmation token.</returns>
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);

        /// <summary>
        /// Attempts to sign in the user with the specified username and password.
        /// </summary>
        /// <param name="model">The LoginViewModel containing the user's credentials.</param>
        /// <returns>A SignInResult indicating the result of the sign-in attempt.</returns>
        Task<SignInResult> LoginAsync(LoginViewModel model);

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task LogoutAsync();

        /// <summary>
        /// Updates the user's details.
        /// </summary>
        /// <param name="user">The ApplicationUser entity to update.</param>
        /// <returns>An IdentityResult indicating the success or failure of the update.</returns>
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);

        /// <summary>
        /// Sets a user's lockout end date.
        /// </summary>
        /// <param name="user">The user to set the lockout for.</param>
        /// <param name="lockoutEnd">The date and time the lockout will end.</param>
        /// <returns>An IdentityResult indicating the success or failure of the lockout operation.</returns>
        Task<IdentityResult> SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd);

        /// <summary>
        /// Checks if a user is in a specific role.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>True if the user is in the role; otherwise, false.</returns>
        Task<bool> IsInRoleAsync(ApplicationUser user, string roleName);

        // --- Data Query Methods ---
        /// <summary>
        /// Finds a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>The ApplicationUser if found; otherwise, null.</returns>
        Task<ApplicationUser> GetUserByEmailasync(string email);

        /// <summary>
        /// Finds a user by their user ID.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The ApplicationUser if found; otherwise, null.</returns>
        Task<ApplicationUser> GetUserByIdAsync(string userId);

        /// <summary>
        /// Gets a list of all users.
        /// </summary>
        /// <returns>A collection of all ApplicationUsers.</returns>
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

        /// <summary>
        /// Gets a SelectList of users, typically for use in dropdown menus.
        /// </summary>
        /// <param name="selectedValue">The selected value for the list.</param>
        /// <returns>A SelectList of users.</returns>
        Task<SelectList> GetUsersForSelectListAsync(object? selectedValue = null);

        /// <summary>
        /// Gets the roles assigned to a specific user.
        /// </summary>
        /// <param name="user">The user to retrieve the roles for.</param>
        /// <returns>A list of role names.</returns>
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);

        //Task<List<ApplicationUser>> GetAllUsersByCompanyIdAsync(string userId);

        /// <summary>
        /// Gets all users belonging to a specific company.
        /// </summary>
        /// <param name="companyId">The ID of the company.</param>
        /// <returns>A collection of ApplicationUsers for that company.</returns>
        Task<IEnumerable<ApplicationUser>> GetUsersByCompanyIdAsync(int companyId);

        /// <summary>
        /// Asynchronously retrieves all active staff members assigned to a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The unique identifier for the condominium.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable of ApplicationUser objects who are staff.
        /// </returns>
        Task<IEnumerable<ApplicationUser>> GetStaffByCondominiumIdAsync(int condominiumId);
        /// <summary>
        /// Asynchronously retrieves all active users associated with a specific company ID.
        /// An active user is one whose DeactivatedAt property is null.
        /// </summary>
        /// <param name="companyId">The unique identifier for the company.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable of ApplicationUser objects who are active.
        /// </returns>
        Task<IEnumerable<ApplicationUser>> GetActiveUsersByCompanyIdAsync(int companyId);

        /// <summary>
        /// Asynchronously retrieves all inactive users associated with a specific company ID.
        /// An inactive user is one whose DeactivatedAt property has a value.
        /// </summary>
        /// <param name="companyId">The unique identifier for the company.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable of ApplicationUser objects who are inactive.
        /// </returns>
        Task<IEnumerable<ApplicationUser>> GetInactiveUsersByCompanyIdAsync(int companyId);

        /// <summary>
        /// Asynchronously retrieves all inactive users of a specific role for a given company.
        /// </summary>
        /// <param name="companyId">The unique identifier for the company.</param>
        /// <param name="roleName">The name of the role to filter by.</param>
        /// <returns>A collection of inactive ApplicationUser objects matching the criteria.</returns>
        Task<IEnumerable<ApplicationUser>> GetInactiveUsersByCompanyAndRoleAsync(int companyId, string roleName);

        /// <summary>
        /// Returns active (non-deactivated) users by company and role.
        /// </summary>
        /// <param name="companyId">Company identifier.</param>
        /// <param name="role">Target role name (e.g., "Condominium Staff").</param>
        /// <returns>Collection of active users in the given role.</returns>
        Task<IEnumerable<ApplicationUser>> GetActiveUsersByCompanyAndRoleAsync(int companyId, string role);

    }
}
