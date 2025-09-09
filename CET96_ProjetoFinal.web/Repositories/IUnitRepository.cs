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
    }
}
