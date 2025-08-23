using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    // It now inherits from the generic repository, specifying its Entity and DbContext

    /// <summary>
    /// Implements the ICondominiumRepository, handling all data access logic for the
    /// Condominium entity using the CondominiumDataContext.
    /// </summary>
    public class CondominiumRepository : GenericRepository<Condominium, CondominiumDataContext>, ICondominiumRepository
    {
        private readonly CondominiumDataContext _context;

        public CondominiumRepository(CondominiumDataContext context) : base(context)
        {
            //_context = context;
        }

        /// <summary>
        /// Gets all active condominiums associated with a specific parent company.
        /// </summary>
        /// <param name="companyId">The ID of the parent company.</param>
        /// <returns>A collection of the company's active condominiums.</returns>
        public async Task<IEnumerable<Condominium>> GetCondominiumsByCompanyIdAsync(int companyId)
        {
            return await _context.Condominiums
                                 .Where(c => c.CompanyId == companyId && c.IsActive)
                                 .OrderBy(c => c.Name)
                                 .ToListAsync();
        }

        /// <summary>
        /// Gets a single condominium by its ID.
        /// </summary>
        /// <param name="id">The ID of the condominium to find.</param>
        /// <returns>The Condominium entity, or null if not found.</returns>
        public async Task<Condominium> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Condominiums.FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
