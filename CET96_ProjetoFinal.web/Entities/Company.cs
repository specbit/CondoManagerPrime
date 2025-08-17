using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Entities
{
    public class Company
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name of the company is required.")]
        [MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(20)]
        [Required(ErrorMessage = "Tax ID is required.")]
        public string? TaxId { get; set; } // "Número Fiscal"

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(200)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

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
