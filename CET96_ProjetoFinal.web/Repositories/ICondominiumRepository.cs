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
        /// Gets all active condominiums associated with a specific parent company.
        /// </summary>
        /// <param name="companyId">The ID of the parent company.</param>
        /// <returns>A collection of the company's active condominiums.</returns>
        Task<IEnumerable<Condominium>> GetCondominiumsByCompanyIdAsync(int companyId);

        /// <summary>
        /// Gets a single condominium by its ID.
        /// </summary>
        /// <param name="id">The ID of the condominium to find.</param>
        /// <returns>The Condominium entity, or null if not found.</returns>
        Task<Condominium> GetByIdWithDetailsAsync(int id);
    }
}
