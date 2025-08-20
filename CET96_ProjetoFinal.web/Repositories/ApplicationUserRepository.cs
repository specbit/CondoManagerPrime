using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Implements the repository for handling ApplicationUser data operations.
    /// </summary>
    public class ApplicationUserRepository : IApplicationUserRepository
    {
        private readonly ApplicationUserDataContext _context;

        public ApplicationUserRepository(ApplicationUserDataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all users and formats them into a SelectList for use in dropdowns.
        /// </summary>
        /// <param name="selectedValue">The ID of the user to be pre-selected in the dropdown.</param>
        /// <returns>A SelectList object containing all users.</returns>
        public async Task<SelectList> GetUsersForSelectListAsync(object? selectedValue = null)
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync();

            // Use "UserName" for the display text as it's more user-friendly than an ID.
            return new SelectList(users, "Id", "UserName", selectedValue);
        }

        /// <summary>
        /// Asynchronously retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the  <see
        /// cref="ApplicationUser"/> object corresponding to the specified <paramref name="userId"/>,  or <see
        /// langword="null"/> if no user with the given identifier exists.</returns>
        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
