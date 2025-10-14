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
        public DbSet<Unit> Units { get; set; }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }


        /// <summary>
        /// Configures the model for the database context by defining entity relationships, indexes, and ignored
        /// properties.
        /// </summary>
        /// <remarks>This method is used to define entity relationships, composite unique indexes, and
        /// ignored properties for the entities managed by this context. It ensures proper configuration of the database
        /// schema and resolves potential conflicts with other contexts managing overlapping entities.</remarks>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure the model for the context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Define relationships and indexes for entities managed by THIS context ---

            // Define the one-to-many relationship
            modelBuilder.Entity<Condominium>()
                .HasMany(c => c.Units) 
                .WithOne(u => u.Condominium)
                .HasForeignKey(u => u.CondominiumId);


            // Create a composite unique index
            modelBuilder.Entity<Condominium>()
                .HasIndex(c => new { c.CompanyId, c.PropertyRegistryNumber })
                .IsUnique();


            // This context becomes aware of 'ApplicationUser' because of the new 'Unit.Owner' navigation property.
            // We must explicitly tell this context to IGNORE the 'Company' property on the 'ApplicationUser' entity.
            // This is critical because the relationship between ApplicationUser and Company is already fully configured
            // and managed by the other database context (ApplicationDbContext), and trying to map it here would
            // create a conflict, causing the "Unable to determine the relationship" build error.
            modelBuilder.Entity<ApplicationUser>().Ignore(u => u.Company);


            // Create the composite unique index
            modelBuilder.Entity<Unit>()
                .HasIndex(u => new { u.CondominiumId, u.UnitNumber })
                .IsUnique();

            // Relationship: A Conversation has many Messages
            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId);
        }
    }
}
