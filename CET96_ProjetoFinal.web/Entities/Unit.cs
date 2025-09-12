using System.ComponentModel.DataAnnotations;

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

        // --- AUDIT & STATUS PROPERTIES ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}