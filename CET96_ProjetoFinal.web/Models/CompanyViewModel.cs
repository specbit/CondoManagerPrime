using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class CompanyViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name of the company is required.")]
        [MaxLength(100, ErrorMessage = "The {0} field must not exceed {1} characters.")]
        [Display(Name = "Company Name")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Tax ID is required.")]
        [MaxLength(20, ErrorMessage = "The {0} field must not exceed {1} characters.")]
        [Display(Name = "Tax ID")]
        public string TaxId { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(200, ErrorMessage = "The {0} field must not exceed {1} characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}