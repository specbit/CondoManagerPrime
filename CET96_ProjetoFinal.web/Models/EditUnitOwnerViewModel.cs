using CET96_ProjetoFinal.web.Enums;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class EditUnitOwnerViewModel
    {
        // The ID is essential for identifying which user to update.
        public string Id { get; set; }

        // The CondominiumId is needed for the "Cancel" button link.
        public int CondominiumId { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address (cannot be changed)")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Document Type")]
        public DocumentTypeEnum DocumentType { get; set; }

        [Required]
        [Display(Name = "Identification Document")]
        public string IdentificationDocument { get; set; }
    }
}