using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Defines the contract for the repository that manages Unit entities.
    /// </summary>
    public interface IUnitRepository : IGenericRepository<Unit>
    {
        /// <summary>
        /// Asynchronously retrieves all units associated with a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The unique identifier of the parent condominium.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a 
        /// collection of Unit entities.
        /// </returns>
        Task<IEnumerable<Unit>> GetUnitsByCondominiumIdAsync(int condominiumId);

        /// <summary>
        /// Checks if a Unit with a specific number already exists within a given condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium to check within.</param>
        /// <param name="unitNumber">The unit number to check for.</param>
        /// <param name="excludeUnitId">An optional ID of a unit to exclude from the check (used for editing).</param>
        /// <returns>A boolean value: true if the unit number exists for another unit, otherwise false.</returns>
        Task<bool> UnitNumberExistsAsync(int condominiumId, string unitNumber, int? excludeUnitId = null);
    }
}
