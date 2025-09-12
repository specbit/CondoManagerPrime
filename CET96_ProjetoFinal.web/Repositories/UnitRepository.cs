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
                .Where(u => u.CondominiumId == condominiumId && u.IsActive)
                .OrderBy(u => u.UnitNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Checks if a Unit with a specific number already exists within a given condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium to check within.</param>
        /// <param name="unitNumber">The unit number to check for.</param>
        /// <param name="excludeUnitId">An optional ID of a unit to exclude from the check (used for editing).</param>
        /// <returns>A boolean value: true if the unit number exists for another unit, otherwise false.</returns>
        public async Task<bool> UnitNumberExistsAsync(int condominiumId, string unitNumber, int? excludeUnitId = null)
        {
            var query = _context.Units
                .Where(u => u.CondominiumId == condominiumId && u.UnitNumber == unitNumber);

            // If an ID to exclude is provided, add another condition to the query.
            if (excludeUnitId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUnitId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
