using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Defines the contract for the condominium repository, extending the generic
    /// repository with custom, condominium-specific data access methods.
    /// </summary>
    public interface ICondominiumRepository : IGenericRepository<Condominium>
    {
        /// <summary>
        /// Gets all ACTIVE condominiums associated with a specific parent company.
        /// </summary>
        /// <param name="companyId">The ID of the parent company.</param>
        /// <returns>A collection of the company's active condominiums.</returns>
        Task<IEnumerable<Condominium>> GetActiveCondominiumsByCompanyIdAsync(int companyId);

        /// <summary>
        /// Gets all INACTIVE (soft-deleted) condominiums for a specific company.
        /// </summary>
        /// <param name="companyId">The ID of the parent company.</param>
        /// <returns>A collection of the company's inactive condominiums.</returns>
        Task<IEnumerable<Condominium>> GetInactiveByCompanyIdAsync(int companyId);

        /// <summary>
        /// Retrieves a list of all unassigned condominiums (where CondominiumManagerId is null)
        /// that belong to any active company created by a specific Company Administrator.
        /// </summary>
        /// <param name="companyAdminId">The user ID of the Company Administrator whose companies are to be queried.</param>
        /// <returns>A task that represents the asynchronous operation and contains a list of unassigned Condominium entities.</returns>
        Task<List<Condominium>> GetUnassignedCondominiumsByCompanyAdminAsync(string companyAdminId);

        /// <summary>
        /// Checks if a condominium with the specified address already exists for a given company.
        /// </summary>
        /// <param name="companyId">The ID of the company to search within.</param>
        /// <param name="address">The address to check for.</param>
        /// <param name="excludeCondominiumId">Optional ID of a condominium to exclude from the check (used for updates).</param>
        /// <returns>True if the address exists for another record; otherwise, false.</returns>
        Task<bool> AddressExistsAsync(int companyId, string address, int? excludeCondominiumId = null);

        /// <summary>
        /// Checks if a condominium with the specified property registry number already exists for a given company.
        /// </summary>
        /// <param name="companyId">The ID of the company to search within.</param>
        /// <param name="registryNumber">The property registry number to check for.</param>
        /// <param name="excludeCondominiumId">Optional ID of a condominium to exclude from the check (used for updates).</param>
        /// <returns>True if the registry number exists for another record; otherwise, false.</returns>
        Task<bool> RegistryNumberExistsAsync(int companyId, string registryNumber, int? excludeCondominiumId = null);

        /// <summary>
        /// Finds the single condominium assigned to a specific manager.
        /// </summary>
        /// <param name="managerId">The ID of the Condominium Manager.</param>
        /// <returns>The Condominium entity if an assignment is found; otherwise, null.</returns>
        Task<Condominium?> GetCondominiumByManagerIdAsync(string managerId);

        /// <summary>
        /// Overrides the base GetByIdAsync method to specifically include the Units collection
        /// when retrieving a single Condominium. This is crucial for displaying the unit count.
        /// </summary>
        /// <param name="id">The ID of the condominium to retrieve.</param>
        /// <returns>The Condominium entity with its Units collection loaded, or null if not found.</returns>
        Task<Condominium?> GetByIdAsync(int id);

        /// <summary>
        /// Gets ALL condominiums (active and inactive) for a specific company.
        /// </summary>
        /// <param name="companyId">The ID of the parent company.</param>
        /// <returns>A collection of all condominiums for the company.</returns>
        Task<IEnumerable<Condominium>> GetCondominiumsByCompanyIdAsync(int companyId);
    }
}
