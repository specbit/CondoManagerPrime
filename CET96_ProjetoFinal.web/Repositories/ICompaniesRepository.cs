using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CET96_ProjetoFinal.web.Repositories
{
    public interface ICompaniesRepository : IGenericRepository<Company>
    {
        Task<IEnumerable<Company>> GetAllWithCreatorsAsync();

        Task<Company?> GetByIdWithCreatorAsync(int id);

        Task<IEnumerable<Company>> GetCompaniesByUserIdAsync(string userId);
    }
}