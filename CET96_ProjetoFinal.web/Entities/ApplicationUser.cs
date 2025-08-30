using CET96_ProjetoFinal.web.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Identification Document is required.")]
        [StringLength(20, ErrorMessage = "Identification Document cannot exceed 20 characters.")]
        public string IdentificationDocument { get; set; }

        [Required(ErrorMessage = "Document Type is required.")]
        [EnumDataType(typeof(DocumentTypeEnum), ErrorMessage = "Invalid Document Type.")]
        public DocumentTypeEnum DocumentType { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number format.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email format.")]
        public override string Email { get; set; }

        // This is the link to the company. It can be null (nullable, with '?') because
        // a user exists before their company is created.
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        // This will temporarily hold the company name from the registration form.
        [MaxLength(100)]
        public string? CompanyName { get; set; }

        // --- Full Audit Trail ---
        public DateTime? DeactivatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DeactivatedByUserId { get; set; }
        public string? UserUpdatedId { get; set; }
        public string? UserCreatedId { get; set; }
    }
}
