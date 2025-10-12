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
