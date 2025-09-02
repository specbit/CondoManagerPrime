using CET96_ProjetoFinal.web.Enums;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents the data required to create a new Condominium Staff member
    /// from the Condominium Manager's dashboard.
    /// </summary>
    public class RegisterCondominiumStaffViewModel
    {
        // This ID is crucial for associating the new staff member with the correct condominium.
        // It will be passed in a hidden field on the form.
        [Required]
        public int CondominiumId { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email (will be used as Username)")]
        public string Username { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Document Type")]
        public DocumentTypeEnum DocumentType { get; set; }

        [Required]
        [Display(Name = "Identification Document")]
        [StringLength(30)]
        public string IdentificationDocument { get; set; }

        [Required]
        [Display(Name = "Profession/Role")]
        [StringLength(50, ErrorMessage = "The profession cannot exceed 50 characters.")]
        public string Profession { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}