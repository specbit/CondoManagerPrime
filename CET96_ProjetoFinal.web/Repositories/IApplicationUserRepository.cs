using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Defines the contract for a repository that handles ApplicationUser data operations.
    /// </summary>
    public interface IApplicationUserRepository
    {
        /// <summary>
        /// Retrieves all users formatted for a dropdown list.
        /// </summary>
        /// <param name="selectedValue">The value to be pre-selected in the list.</param>
        /// <returns>A SelectList object containing all users.</returns>
        Task<SelectList> GetUsersForSelectListAsync(object? selectedValue = null);

        /// <summary>
        /// Asynchronously retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the  <see
        /// cref="ApplicationUser"/> object corresponding to the specified <paramref name="userId"/>,  or <see
        /// langword="null"/> if no user with the given identifier exists.</returns>
        Task<ApplicationUser> GetUserByIdAsync(string userId);
    }
}
