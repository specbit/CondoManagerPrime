using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Identity;

namespace CET96_ProjetoFinal.web.Helpers
{
    public interface IApplicationUserHelper
    {
        /// <summary>
        /// Asynchronously retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the  <see
        /// cref="ApplicationUser"/> associated with the specified email, or <see langword="null"/>  if no user is
        /// found.</returns>
        Task<ApplicationUser> GetUserByEmailasync(string email);
        
        /// <summary>
        /// Asynchronously adds a new user to the system with the specified password.
        /// </summary>
        /// <remarks>The method validates the provided password against the system's password policy.  If
        /// the operation fails, the returned <see cref="IdentityResult"/> will contain error details.</remarks>
        /// <param name="user">The user to add. Must not be <see langword="null"/>.</param>
        /// <param name="password">The password for the new user. Must meet the system's password requirements.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.  The task result contains an <see
        /// cref="IdentityResult"/> indicating whether the operation succeeded.</returns>
        Task<IdentityResult> AddUserAsync(ApplicationUser user, string password);
        
        /// <summary>
        /// Checks whether the specified role exists in the system and performs any necessary actions if it does not.
        /// </summary>
        /// <remarks>This method is typically used to ensure that a required role is present in the system
        /// before performing role-based operations.</remarks>
        /// <param name="roleName">The name of the role to check. This value cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CheckRoleAsync(string roleName);
        
        /// <summary>
        /// Asynchronously adds the specified user to the given role.
        /// </summary>
        /// <remarks>This method assigns the specified role to the user. Ensure that the role exists
        /// before calling this method.</remarks>
        /// <param name="user">The user to be added to the role. Cannot be <see langword="null"/>.</param>
        /// <param name="roleName">The name of the role to which the user will be added. Cannot be <see langword="null"/> or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddUserToRoleAsync(ApplicationUser user, string roleName);
        
        /// <summary>
        /// Attempts to authenticate a user based on the provided login information.
        /// </summary>
        /// <remarks>Ensure that the <see cref="LoginViewModel"/> contains valid and properly formatted
        /// credentials before calling this method. The method may return results indicating partial success, such as
        /// requiring further authentication steps.</remarks>
        /// <param name="model">The login details, including username and password, encapsulated in a <see cref="LoginViewModel"/> object.
        /// Cannot be null.</param>
        /// <returns>A <see cref="SignInResult"/> indicating the outcome of the authentication attempt.  Possible results include
        /// success, failure, or additional actions required (e.g., two-factor authentication).</returns>
        Task<SignInResult> LoginAsync(LoginViewModel model);

        /// <summary>
        /// Logs the user out of the application asynchronously.
        /// </summary>
        /// <remarks>This method clears the user's session and any associated authentication tokens.  It
        /// should be called when the user explicitly requests to log out or when the application  needs to terminate
        /// the user's session for security reasons.</remarks>
        /// <returns>A task that represents the asynchronous logout operation.</returns>
        Task LogoutAsync();

        /// <summary>
        /// Asynchronously retrieves all users in the system.
        /// </summary>
        /// <remarks>The returned collection may be empty if no users are found. This method does not
        /// filter or  paginate the results; all users are included in the returned collection.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an  IEnumerable{T} of
        /// ApplicationUser objects representing all users.</returns>
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

        /// <summary>
        /// Asynchronously retrieves the list of roles associated with the specified user.
        /// </summary>
        /// <param name="user">The user for whom to retrieve the roles. Cannot be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of role names associated
        /// with the user. The list will be empty if the user has no roles.</returns>
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);

        /// <summary>
        /// Updates a user's profile information.
        /// </summary>
        /// <param name="user">The user entity with updated information.</param>
        /// <returns>The result of the update operation.</returns>
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        /// <param name="user">The user whose password will be changed.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The new password for the user.</param>
        /// <returns>The result of the password change operation.</returns>
        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string oldPassword, string newPassword);

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <remarks>Use this method to fetch user details when you have the user's unique identifier.
        /// Ensure that the <paramref name="userId"/> is valid and not null or empty before calling this
        /// method.</remarks>
        /// <param name="userId">The unique identifier of the user to retrieve. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ApplicationUser">
        /// corresponding to the specified <paramref name="userId"/>, or <see langword="null"/> if no user is found.</returns>
        Task<ApplicationUser> GetUserByIdAsync(string userId);

        /// <summary>
        /// Confirms a user's email address using the provided confirmation token.
        /// </summary>
        /// <remarks>This method validates the provided token and updates the user's email confirmation
        /// status if the token is valid. If the token is invalid or expired, the operation will fail, and the returned
        /// <see cref="IdentityResult"/> will contain error details.</remarks>
        /// <param name="user">The user whose email address is being confirmed. Cannot be null.</param>
        /// <param name="token">The email confirmation token associated with the user. Cannot be null or empty.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation.  The task result contains an <see
        /// cref="IdentityResult"/> indicating whether the email confirmation was successful.</returns>
        Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token);

        /// <summary>
        /// Generates a token that can be used to confirm the email address of the specified user.
        /// </summary>
        /// <param name="user">The user for whom the email confirmation token is generated. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the email confirmation token as
        /// a string.</returns>
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
    }
}
