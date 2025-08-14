using CET96_ProjetoFinal.web.Enums;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class RegisterCompanyAdminViewModel
    {
        // --- User Details ---
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email (will be your username)")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        // --- Company Details ---
        [Required]
        [Display(Name = "Company Name")]
        [MaxLength(100)]
        public string CompanyName { get; set; }

        // --- Required User Details ---
        [Required]
        [Display(Name = "Document Type")]
        public DocumentTypeEnum DocumentType { get; set; }

        [Required]
        [Display(Name = "Document ID Number")]
        public string IdentificationDocument { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}