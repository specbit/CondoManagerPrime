using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CET96_ProjetoFinal.web.Entities
{
    /// <summary>
    /// Represents a single condominium managed by a company.
    /// This is the primary entity for the Condominium database.
    /// </summary>
    public class Condominium : IEntity
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; } // FK column
        [NotMapped] // EF Core will completely ignore this property (Company lives in another DbContext, so we load it manually)
        public Company Company { get; set; } // Navigation property

        [Display(Name = "Condominium Manager")]
        public string? CondominiumManagerId { get; set; }
        [NotMapped]
        public ApplicationUser? CondominiumManager { get; set; }

        [Required(ErrorMessage = "The Condominium Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Condominium Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Address is required.")]
        [MaxLength(200)]
        public string Address { get; set; }

        [Required(ErrorMessage = "The Zip Code is required.")]
        [MaxLength(20)]
        [Display(Name = "Zip Code")]

        public string ZipCode { get; set; }
        [Required(ErrorMessage = "The City is required.")]
        [MaxLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "The Property Registry Number is required.")]
        [MaxLength(50)]
        [Display(Name = "Property Registry Number")]
        public string PropertyRegistryNumber { get; set; }

        //[Required(ErrorMessage = "The Number of Units is required.")]
        //[Display(Name = "Number of Units")]
        //public int NumberOfUnits { get; set; }

        [Required(ErrorMessage = "The Contract Value is required.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Contract Value")]
        public decimal ContractValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Fee Per Unit")]
        public decimal FeePerUnit { get; set; }

        // --- Audit Fields ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string UserCreatedId { get; set; }
        public string? UserUpdatedId { get; set; }
        public string? UserDeletedId { get; set; }

        // Navigation property - One Condominium has many Units
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}
