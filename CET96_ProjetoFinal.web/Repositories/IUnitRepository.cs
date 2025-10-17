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


        /// <summary>
        /// Checks if a user is already assigned as an owner to any unit in the database.
        /// </summary>
        /// <param name="ownerId">The string ID of the user (ApplicationUser) to check.</param>
        /// <returns>Returns <c>true</c> if the user is assigned to at least one unit; otherwise, <c>false</c>.</returns>
        bool IsOwnerAssigned(string ownerId);


        /// <summary>
        /// Asynchronously retrieves a single Unit entity by its ID, including its related Condominium.
        /// </summary>
        /// <remarks>
        /// This method eagerly loads only the 'Condominium' navigation property from the same database context.
        /// The 'Owner' details must be fetched separately from the other database context and manually
        /// attached to the returned Unit object in the controller.
        /// </remarks>
        /// <param name="id">The primary key of the Unit to retrieve.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. 
        /// The task result contains the Unit object with its Condominium details if found; otherwise, null.
        /// </returns>
        Task<Unit?> GetUnitWithDetailsAsync(int id);


        /// <summary>
        /// Asynchronously retrieves the single Unit assigned to a specific owner, including related condominium details.
        /// </summary>
        /// <param name="ownerId">The string identifier (GUID) of the owner whose assigned unit is to be retrieved.</param>
        /// <returns>
        /// A Task that represents the asynchronous operation. 
        /// The task result contains the Unit object with its Condominium details if an assignment is found; otherwise, null.
        /// </returns>
        Task<Unit> GetUnitByOwnerIdWithDetailsAsync(string ownerId);
    }
}
