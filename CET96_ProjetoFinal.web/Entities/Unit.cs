using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CET96_ProjetoFinal.web.Entities
{
    public class Unit : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitNumber { get; set; } // e.g., "1A", "Floor 3, Apt B"

        // Links to the Condominium (Many Units belong to One Condominium)
        [Required]
        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        // Links to the Owner user. This is just an ID since the User lives in another database.
        public string? OwnerId { get; set; }
        [NotMapped]
        public ApplicationUser? Owner { get; set; } // Navigation property for the owner

        // --- AUDIT & STATUS PROPERTIES ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public string UserCreatedId { get; set; }
        public string? UserUpdatedId { get; set; }
        public string? UserDeletedId { get; set; }
    }
}