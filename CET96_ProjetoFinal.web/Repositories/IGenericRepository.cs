using CET96_ProjetoFinal.web.Entities;
using System.Linq.Expressions;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Defines the contract for a generic repository, prioritizing performance and flexibility.
    /// </summary>
    /// <typeparam name="T">The entity type, which must be a class and implement IEntity.</typeparam>
    public interface IGenericRepository<T> where T : class, IEntity
    {
        /// <summary>
        /// Gets all entities of type T for read-only purposes.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Gets a single entity by its ID for modification or deletion (with tracking).
        /// </summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Finds the first entity that matches a specified condition.
        /// </summary>
        /// <param name="predicate">The condition to test each element for.</param>
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds a new entity to the context.
        /// </summary>
        Task CreateAsync(T entity);

        /// <summary>
        /// Marks an existing entity as modified in the context.
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Marks an existing entity for deletion. This is the most performant approach.
        /// </summary>
        void Delete(T entity);

        /// <summary>
        /// Checks if an entity with the specified ID exists.
        /// </summary>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Saves all pending changes to the data store.
        /// </summary>
        Task<bool> SaveAllAsync();
    }
}
