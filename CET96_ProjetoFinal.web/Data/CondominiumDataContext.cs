using CET96_ProjetoFinal.web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Data
{
    /// <summary>
    /// The Entity Framework Core DbContext for the condominium-specific database.
    /// </summary>
    /// <remarks>
    /// This context is responsible for managing all entities related to the operational
    /// side of a condominium, such as the Condominium itself, Fractions, Incidents, etc.
    /// </remarks>
    public class CondominiumDataContext : DbContext
    {
        public CondominiumDataContext(DbContextOptions<CondominiumDataContext> options) : base(options)
        {
        }

        public DbSet<Condominium> Condominiums { get; set; }

        // Later, I will add other DbSets here, like Fractions, Incidents, etc.
    }
}
