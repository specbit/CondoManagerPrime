using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    // Why do we need a ViewModel?
    // A ViewModel (like this one) is a best practice that separates our database structure (the 'Entity')
    // from what our user sees on the screen (the 'View'). It gives us three main benefits:
    // 1. Security: We only include the fields the user is allowed to see or edit, hiding sensitive
    //    or system-managed data like 'IsActive' or 'CreatedAt'.
    // 2. Simplicity: The ViewModel is tailored specifically for the form, making the code in the View cleaner.
    // 3. Validation: We can add specific validation rules (like [Display(Name = ...)]) that are
    //    only relevant to the user interface, keeping our database entity clean.

    /// <summary>
    /// Represents the data required for creating or editing a company. This model is used
    /// to transfer data between the controller and the company forms (Views).
    /// </summary>
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
        [RegularExpression(@"^\d+$", ErrorMessage = "Tax ID must contain digits only.")]
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