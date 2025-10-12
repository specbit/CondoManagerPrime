using CET96_ProjetoFinal.web.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents the view model for a unit owner, containing information for creating or editing a unit owner and
    /// supporting data for rendering the view.
    /// </summary>
    /// <remarks>This view model is used to capture and display information about a unit owner, including
    /// personal details, contact information, and associated condominium data. It also includes properties for managing
    /// view-specific behavior, such as dropdown lists and validation.</remarks>
    public class UnitOwnerViewModel
    {
        /// <summary>
        /// The user's unique ID. It is null when creating a new owner and populated when editing an existing one.
        /// </summary>
        public string? Id { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [Display(Name = "Condominium")]
        public int CondominiumId { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

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

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        // --- Properties for the View ---
        public IEnumerable<SelectListItem>? CondominiumsList { get; set; }
        public bool CanPickCondominium { get; set; }
    }
}   