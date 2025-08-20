using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    public class CompanyRepository : GenericRepository<Company>, ICompaniesRepository
    {
        private readonly ApplicationUserDataContext _context;

        public CompanyRepository(ApplicationUserDataContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all companies from the database, including their creator's data.
        /// </summary>
        /// <returns>A collection of all Company entities with their creators.</returns>
        public async Task<IEnumerable<Company>> GetAllWithCreatorsAsync()
        {
            return await _context.Companies
                                 .Include(company => company.ApplicationUser) // Eagerly loads the Creator
                                 .AsNoTracking() // Improves performance for read-only lists
                                 .ToListAsync();
        }

        /// <summary>
        /// Gets a single company by its ID, including its creator's data.
        /// </summary>
        /// <param name="id">The ID of the company to find.</param>
        /// <returns>The Company entity with its Creator, or null if not found.</returns>
        public async Task<Company?> GetByIdWithCreatorAsync(int id)
        {
            return await _context.Companies
                                 .Include(company => company.ApplicationUser) // Eagerly loads the Creator
                                 .FirstOrDefaultAsync(company => company.Id == id);
        }

        /// <summary>
        /// Retrieves a collection of active companies associated with the specified user.
        /// </summary>
        /// <remarks>This method only returns companies that are marked as active. Ensure that the  userId
        /// corresponds to a valid user in the system.</remarks>
        /// <param name="userId">The unique identifier of the user whose associated active companies are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an  IEnumerable{T} of Company
        /// objects, ordered by name,  that are associated with the specified user and marked as active.</returns>
        public async Task<IEnumerable<Company>> GetCompaniesByUserIdAsync(string userId)
        {
            // This query finds all companies that were created by the specified user
            // and are also marked as active (for your "Deactivate" feature).
            return await _context.Companies
                                 .Where(c => c.ApplicationUserId == userId && c.IsActive)
                                 .OrderBy(c => c.Name)
                                 .ToListAsync();
        }
    }
}
