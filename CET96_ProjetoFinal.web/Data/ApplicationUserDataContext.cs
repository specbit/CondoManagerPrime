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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // This block defines the main one-to-many relationship:
            // One Company has many Users.
            builder.Entity<Company>()
                .HasMany(c => c.Users) // A Company has a collection of Users
                .WithOne(u => u.Company) // Each User has one Company
                .HasForeignKey(u => u.CompanyId); // The foreign key is in the ApplicationUser table

            // This block defines the one-to-one relationship for the company's creator.
            // It prevents cascading deletes, so if you delete a user, their company isn't automatically deleted.
            builder.Entity<Company>()
                .HasOne(c => c.ApplicationUser)
                .WithMany()
                .HasForeignKey(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // DbSet for the Company entity
        public DbSet<Company> Companies { get; set; }
    }
}
