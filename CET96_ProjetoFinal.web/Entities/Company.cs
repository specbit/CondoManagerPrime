using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Entities
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(20)]
        public string? TaxId { get; set; } // "Número Fiscal"

        // This property indicates whether the payment has been validated.
        public bool PaymentValidated { get; set; } = false; // Defaults to false

        // Links the Company to the first admin who created it.
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        // This is the property that allows a company to have a list of all its users.
        public ICollection<ApplicationUser> Users { get; set; }

        // --- Audit Fields ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // It's good practice to track which user made changes.
        // These can be null if the system performs an action.
        public string? UserCreatedId { get; set; }
        public string? UserUpdatedId { get; set; }
    }
}
