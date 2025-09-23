using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Repositories
{
    /// <summary>
    /// Defines the contract for a repository that handles data operations for the Company entity.
    /// It includes standard CRUD operations from the generic repository and custom,
    /// company-specific query methods.
    /// </summary>
    public interface ICompanyRepository : IGenericRepository<Company>
    {
        /// <summary>
        /// Gets all companies from the database, eagerly loading their creator's data.
        /// </summary>
        /// <returns>A collection of all Company entities with their creators loaded.</returns>
        Task<IEnumerable<Company>> GetAllWithCreatorsAsync();

        /// <summary>
        /// Gets a single company by its ID, eagerly loading its creator's data.
        /// </summary>
        /// <param name="id">The ID of the company to find.</param>
        /// <returns>The Company entity with its creator, or null if not found.</returns>
        Task<Company?> GetByIdWithCreatorAsync(int id);

        /// <summary>
        /// Gets all active companies created by a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose companies to retrieve.</param>
        /// <returns>A collection of the user's active companies.</returns>
        Task<IEnumerable<Company>> GetCompaniesByUserIdAsync(string userId);
        // --- Methods for CREATE validation (take 1 argument) ---
        /// <summary>
        /// Checks if a company name is already in use.
        /// </summary>
        /// <param name="name">The company name to check.</param>
        /// <returns>True if the name is in use; otherwise, false.</returns>
        Task<bool> IsNameInUseAsync(string name);

        /// <summary>
        /// Checks if a company Tax ID is already in use.
        /// </summary>
        /// <param name="taxId">The Tax ID to check.</param>
        /// <returns>True if the Tax ID is in use; otherwise, false.</returns>
        Task<bool> IsTaxIdInUseAsync(string taxId);

        /// <summary>
        /// Checks if a company email address is already in use.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if the email is in use; otherwise, false.</returns>
        Task<bool> IsEmailInUseAsync(string email);

        /// <summary>
        /// Checks if a company phone number is already in use.
        /// </summary>
        /// <param name="phoneNumber">The phone number to check.</param>
        /// <returns>True if the phone number is in use; otherwise, false.</returns>
        Task<bool> IsPhoneNumberInUseAsync(string phoneNumber);
        // --- END Methods for CREATE validation (take 1 argument) ---

        // --- Methods for EDIT validation (take 2 arguments) ---
        Task<bool> IsNameInUseAsync(string name, int companyIdToExclude);
        Task<bool> IsTaxIdInUseAsync(string taxId, int companyIdToExclude);
        Task<bool> IsEmailInUseAsync(string email, int companyIdToExclude);
        Task<bool> IsPhoneNumberInUseAsync(string phoneNumber, int companyIdToExclude);
        // --- END Methods for EDIT validation (take 2 arguments) ---

        /// <summary>
        /// Gets all inactive companies created by a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose inactive companies to retrieve.</param>
        /// <returns>A collection of the user's inactive companies.</returns>
        Task<IEnumerable<Company>> GetInactiveCompaniesByUserIdAsync(string userId);

        /// <summary>
        /// Gets ALL companies (active and inactive) associated with a specific Company Administrator.
        /// </summary>
        /// <param name="adminId">The ID of the Company Administrator (user ID).</param>
        /// <returns>A collection of all companies managed by the administrator.</returns>
        Task<IEnumerable<Company>> GetAllCompaniesByAdminIdAsync(string adminId);

        Task<bool> DoesCompanyExistForUserAsync(string userId);
    }
}