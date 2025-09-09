using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Implements the ICondominiumRepository, handling all data access logic for the
    /// Condominium entity using the CondominiumDataContext.
    /// </summary>
    public class CondominiumRepository : GenericRepository<Condominium, CondominiumDataContext>, ICondominiumRepository
    {
        // This context is for cross-database queries involving Users/Companies.
        private readonly ApplicationUserDataContext _userContext;

        /// <summary>
        /// Initializes a new instance of the CondominiumRepository.
        /// </summary>
        /// <param name="condominiumDataContext">The DbContext for condominiums, passed to the base generic repository.</param>
        /// <param name="userContext">The DbContext for users and companies.</param>
        public CondominiumRepository(CondominiumDataContext condominiumDataContext, ApplicationUserDataContext userContext) : base(condominiumDataContext)
        {
            _userContext = userContext;
        }

        /// <summary>
        /// Gets all active condominiums associated with a specific parent company.
        /// </summary>
        /// <param name="companyId">The ID of the parent company.</param>
        /// <returns>A collection of the company's active condominiums.</returns>
        public async Task<IEnumerable<Condominium>> GetActiveCondominiumsByCompanyIdAsync(int companyId)
        {
            return await _context.Condominiums
                                .Include(c => c.Units) // Include units
                                .Where(c => c.CompanyId == companyId && c.IsActive)
                                .OrderBy(c => c.Name)
                                .AsNoTracking() // Read-only operation
                                .ToListAsync();
        }

        /// <summary>
        /// Gets all INACTIVE (soft-deleted) condominiums for a specific company.
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Condominium>> GetInactiveByCompanyIdAsync(int companyId)
        {
            return await _context.Condominiums
                                 .Where(c => c.CompanyId == companyId && !c.IsActive)
                                 .OrderBy(c => c.Name)
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        /// <summary>
        /// Retrieves a list of all unassigned condominiums for a specific Company Administrator.
        /// </summary>
        public async Task<List<Condominium>> GetUnassignedCondominiumsByCompanyAdminAsync(string companyAdminId)
        {
            // 1. Get company IDs from the Users database (_userContext)
            var companyIds = await _userContext.Companies
                                               .Where(c => c.UserCreatedId == companyAdminId && c.IsActive)
                                               .Select(c => c.Id)
                                               .ToListAsync();

            // 2. Use those IDs to find unassigned condominiums in the Condominiums database (_context)
            return await _context.Condominiums
                                 .Where(c => companyIds.Contains(c.CompanyId) && c.CondominiumManagerId == null)
                                 .OrderBy(c => c.Name)
                                 .AsNoTracking() // Read-only operation
                                 .ToListAsync();
        }
        /// <summary>
        /// Checks if a condominium with the specified address already exists for a given company,
        /// optionally excluding a specific condominium from the search.
        /// </summary>
        /// <param name="companyId">The ID of the company to search within.</param>
        /// <param name="address">The address to check for.</param>
        /// <param name="excludeCondominiumId">An optional ID of a condominium to exclude from the check. This is used during an update operation to prevent a record from matching itself.</param>
        /// <returns>Returns <c>true</c> if the address already exists for another record in the company; otherwise, <c>false</c>.</returns>
        public async Task<bool> AddressExistsAsync(int companyId, string address, int? excludeCondominiumId = null)
        {
            var query = _context.Condominiums
                .Where(c => c.CompanyId == companyId && c.Address == address);

            if (excludeCondominiumId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCondominiumId.Value);
            }
            return await query.AnyAsync();
        }

        /// <summary>
        /// Checks if a condominium with the specified property registry number already exists for a given company,
        /// optionally excluding a specific condominium from the search.
        /// </summary>
        /// <param name="companyId">The ID of the company to search within.</param>
        /// <param name="registryNumber">The property registry number to check for.</param>
        /// <param name="excludeCondominiumId">An optional ID of a condominium to exclude from the check. This is used during an update operation to prevent a record from matching itself.</param>
        /// <returns>Returns <c>true</c> if the registry number already exists for another record in the company; otherwise, <c>false</c>.</returns>
        public async Task<bool> RegistryNumberExistsAsync(int companyId, string registryNumber, int? excludeCondominiumId = null)
        {
            var query = _context.Condominiums
                .Where(c => c.CompanyId == companyId && c.PropertyRegistryNumber == registryNumber);

            if (excludeCondominiumId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCondominiumId.Value);
            }
            return await query.AnyAsync();
        }

        /// <summary>
        /// Finds the single condominium assigned to a specific manager.
        /// </summary>
        /// <param name="managerId">The ID of the Condominium Manager.</param>
        /// <returns>The Condominium entity if an assignment is found; otherwise, null.</returns>
        public async Task<Condominium?> GetCondominiumByManagerIdAsync(string managerId)
        {
            return await _context.Condominiums
                .Include(c => c.Units) // <-- EAGER LOAD THE UNITS
                .FirstOrDefaultAsync(c => c.CondominiumManagerId == managerId && c.IsActive);
        }

        /// <summary>
        /// Overrides the base GetByIdAsync method to specifically include the Units collection
        /// when retrieving a single Condominium. This is crucial for displaying the unit count.
        /// </summary>
        /// <param name="id">The ID of the condominium to retrieve.</param>
        /// <returns>The Condominium entity with its Units collection loaded, or null if not found.</returns>
        public new async Task<Condominium?> GetByIdAsync(int id)
        {
            return await _context.Condominiums
                .Include(c => c.Units) // Eager load the Units
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}