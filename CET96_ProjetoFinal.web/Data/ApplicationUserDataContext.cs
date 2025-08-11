using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Data
{
    public class ApplicationUserDataContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationUserDataContext(DbContextOptions<ApplicationUserDataContext> options) : base(options)
        {
        }
       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional model configurations can be added here
        }

        // DbSet for the Company entity
        public DbSet<Company> Companies { get; set; }
    }
}
