using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Implements the repository for the Company entity, providing both generic CRUD operations
    /// and custom, company-specific data access methods.
    /// </summary>
    /// <remarks>
    /// This class handles all direct database interactions for companies, abstracting the
    /// data layer from the application's business logic.
    /// </remarks>
    public class CompanyRepository : GenericRepository<Company, ApplicationUserDataContext>, ICompanyRepository
    {
        //private readonly ApplicationUserDataContext _context;

        public CompanyRepository(ApplicationUserDataContext context) : base(context)
        {
            //_context = context;
        }

        /// <summary>
        /// Gets all companies from the database, including their creator's data.
        /// </summary>
        /// <returns>A collection of all Company entities with their creators.</returns>
        public async Task<IEnumerable<Company>> GetAllWithCreatorsAsync()
        {
            return await _context.Companies
                                 .Include(company => company.ApplicationUser) // Eagerly loads the Creator
                                 .AsNoTracking() // Improves performance for read-only lists
                                 .ToListAsync();
        }

        /// <summary>
        /// Gets a single company by its ID, including its creator's data.
        /// </summary>
        /// <param name="id">The ID of the company to find.</param>
        /// <returns>The Company entity with its Creator, or null if not found.</returns>
        public async Task<Company?> GetByIdWithCreatorAsync(int id)
        {
            return await _context.Companies
                                 .Include(company => company.ApplicationUser) // Eagerly loads the Creator
                                 .FirstOrDefaultAsync(company => company.Id == id);
        }

        /// <summary>
        /// Retrieves a collection of active companies associated with the specified user.
        /// </summary>
        /// <remarks>This method only returns companies that are marked as active. Ensure that the  userId
        /// corresponds to a valid user in the system.</remarks>
        /// <param name="userId">The unique identifier of the user whose associated active companies are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an  IEnumerable{T} of Company
        /// objects, ordered by name,  that are associated with the specified user and marked as active.</returns>
        public async Task<IEnumerable<Company>> GetCompaniesByUserIdAsync(string userId)
        {
            // This query finds all companies that were created by the specified user
            // and are also marked as active (for "Deactivate" feature).
            return await _context.Companies
                                 .Where(c => c.ApplicationUserId == userId && c.IsActive)
                                 .OrderBy(c => c.Name)
                                 .ToListAsync();
        }

        // --- Methods for CREATE validation (take 1 argument) ---

        /// <summary>
        /// Checks if a company Tax ID is already in use.
        /// </summary>
        /// <param name="taxId">The Tax ID to check for duplicates.</param>
        /// <returns>True if the Tax ID is in use; otherwise, false.</returns>
        public async Task<bool> IsTaxIdInUseAsync(string taxId)
        {
            return await _context.Companies.AnyAsync(c => c.TaxId == taxId);
        }

        /// <summary>
        /// Checks if a company email address is already in use.
        /// </summary>
        /// <param name="email">The email address to check for duplicates.</param>
        /// <returns>True if the email is in use; otherwise, false.</returns>
        public async Task<bool> IsEmailInUseAsync(string email)
        {
            return await _context.Companies.AnyAsync(c => c.Email == email);
        }

        /// <summary>
        /// Checks if a company name is already in use.
        /// </summary>
        /// <param name="name">The company name to check for duplicates.</param>
        /// <returns>True if the name is in use; otherwise, false.</returns>
        public async Task<bool> IsNameInUseAsync(string name)
        {
            return await _context.Companies.AnyAsync(c => c.Name == name);
        }

        /// <summary>
        /// Checks if a company phone number is already in use.
        /// </summary>
        /// <param name="phoneNumber">The phone number to check for duplicates.</param>
        /// <returns>True if the phone number is in use; otherwise, false.</returns>
        public async Task<bool> IsPhoneNumberInUseAsync(string phoneNumber)
        {
            return await _context.Companies.AnyAsync(c => c.PhoneNumber == phoneNumber);
        }
        // --- END Methods for CREATE validation (take 1 argument) ---

        // --- Methods for EDIT validation (take 2 arguments) ---

        public async Task<bool> IsNameInUseAsync(string name, int companyIdToExclude)
        {
            return await _context.Companies.AnyAsync(c => c.Name == name && c.Id != companyIdToExclude);
        }

        public async Task<bool> IsTaxIdInUseAsync(string taxId, int companyIdToExclude)
        {
            return await _context.Companies.AnyAsync(c => c.TaxId == taxId && c.Id != companyIdToExclude);
        }

        public async Task<bool> IsEmailInUseAsync(string email, int companyIdToExclude)
        {
            return await _context.Companies.AnyAsync(c => c.Email == email && c.Id != companyIdToExclude);
        }

        public async Task<bool> IsPhoneNumberInUseAsync(string phoneNumber, int companyIdToExclude)
        {
            return await _context.Companies.AnyAsync(c => c.PhoneNumber == phoneNumber && c.Id != companyIdToExclude);
        }
        // --- END Methods for EDIT validation (take 2 arguments) ---

        /// <summary>
        /// Gets all inactive companies created by a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose inactive companies to retrieve.</param>
        /// <returns>A collection of the user's inactive companies.</returns>
        public async Task<IEnumerable<Company>> GetInactiveCompaniesByUserIdAsync(string userId)
        {
            return await _context.Companies
                                 .Where(c => c.ApplicationUserId == userId && !c.IsActive) // The only change is !c.IsActive
                                 .OrderBy(c => c.Name)
                                 .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Company>> GetAllCompaniesByAdminIdAsync(string adminId)
        {
            // Finds all companies where the admin ID matches, regardless of active status.
            return await _context.Companies
                                 .Where(c => c.ApplicationUserId == adminId)
                                 .ToListAsync();
        }

        public async Task<bool> DoesCompanyExistForUserAsync(string userId)
        {
            return await _context.Companies.AnyAsync(c => c.ApplicationUserId == userId);
        }
    }
}
