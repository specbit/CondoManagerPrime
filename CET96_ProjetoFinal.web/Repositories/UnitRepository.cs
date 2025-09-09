using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Implements the IUnitRepository, handling data access logic for the Unit entity.
    /// </summary>
    public class UnitRepository : GenericRepository<Unit, CondominiumDataContext>, IUnitRepository
    {
        // Don't need a second context here since Units are in the same DB as Condominiums.
        public UnitRepository(CondominiumDataContext context) : base(context)
        {
        }

        /// <summary>
        /// Asynchronously retrieves all units associated with a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The unique identifier of the parent condominium.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a 
        /// collection of Unit entities.
        /// </returns>
        public async Task<IEnumerable<Unit>> GetUnitsByCondominiumIdAsync(int condominiumId)
        {
            return await _context.Units
                .Where(u => u.CondominiumId == condominiumId)
                .OrderBy(u => u.UnitNumber)
                .ToListAsync();
        }
    }
}
