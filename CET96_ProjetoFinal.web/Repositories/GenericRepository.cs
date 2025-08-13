using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// A generic repository for handling database operations for any entity
    /// that implements the IEntity interface.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class, IEntity
    {
        private readonly ApplicationUserDataContext _context;

        public GenericRepository(ApplicationUserDataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all entities of type T from the database without tracking for read-only lists.
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Gets a single entity by its ID with tracking enabled, as it's typically fetched to be updated or deleted.
        /// </summary>
        public async Task<T?> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <summary>
        /// Finds the first entity that matches a specified condition using a lambda expression.
        /// </summary>
        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
        {
            // IMPORTANT: Want to use this!!!
            // Find a user by their email without adding a "GetByEmail" method to the repository
            //var user = await _userRepository.FindAsync(u => u.Email == "test@example.com");
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Adds a new entity to the context. Changes are not saved until SaveAllAsync is called.
        /// </summary>
        public async Task CreateAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        /// <summary>
        /// Attaches an entity to the context and marks it as modified.
        /// </summary>
        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        /// <summary>
        /// Marks an entity for deletion. This is more efficient as it avoids a database lookup if the entity is already loaded.
        /// </summary>
        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        /// <summary>
        /// Checks if an entity with the specified ID exists in the database.
        /// </summary>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Set<T>().AnyAsync(e => e.Id == id);
        }

        /// <summary>
        /// Saves all pending changes in the context to the database.
        /// </summary>
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
