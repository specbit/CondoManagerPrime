using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class CompanyViewModel
    {
        [Required]
        [MaxLength(100, ErrorMessage = "The {0} field must have a maximum of {1} characters.")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        [Display(Name = "Tax ID")]
        [MaxLength(20, ErrorMessage = "The {0} field must have a maximum of {1} characters.")]
        public string TaxId { get; set; }
    }
}