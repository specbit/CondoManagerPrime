using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class CreateUnitViewModel
    {
        [Required]
        [MaxLength(50)]
        [Display(Name = "Unit Number or Name")]
        public string UnitNumber { get; set; } // e.g., "1A", "Floor 3, Apt B"

        // This will be a hidden field in the form
        public int CondominiumId { get; set; }
    }
}
