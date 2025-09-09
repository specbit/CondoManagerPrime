using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CET96_ProjetoFinal.web.Models
{
    // Why do we need a ViewModel?
    // A ViewModel separates our database structure (the 'Entity') from what the user sees
    // on the screen (the 'View'). This provides:
    // 1. Security: We only include fields the user is allowed to edit, hiding system data.
    // 2. Simplicity: The model is tailored specifically for the form, making the View cleaner.
    // 3. Validation: We can add UI-specific validation and data (like a dropdown list of managers).

    /// <summary>
    /// Represents the data required for the Create and Edit condominium forms.
    /// </summary>
    public class CondominiumViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Company Id")]
        public int CompanyId { get; set; }

        [Display(Name = "Condominium Manager")]
        public string? CondominiumManagerId { get; set; }

        // 3. Data annotations copied from the entity
        [Required(ErrorMessage = "The Condominium Name is required.")]
        [MaxLength(100, ErrorMessage = "The Name cannot exceed 100 characters.")]
        [Display(Name = "Condominium Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Address is required.")]
        [MaxLength(200, ErrorMessage = "The Address cannot exceed 200 characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "The City is required.")]
        [MaxLength(50, ErrorMessage = "The City cannot exceed 50 characters.")]
        public string City { get; set; }

        [Required(ErrorMessage = "The Zip Code is required.")]
        [MaxLength(20, ErrorMessage = "The Zip Code cannot exceed 20 characters.")]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "The Property Registry Number is required.")]
        [MaxLength(50, ErrorMessage = "The Property Registry Number cannot exceed 50 characters.")]
        [Display(Name = "Property Registry Number")]
        public string PropertyRegistryNumber { get; set; }

        //[Required(ErrorMessage = "The Number of Units is required.")]
        //[Range(1, int.MaxValue, ErrorMessage = "Number of Units must be at least 1.")]
        //[Display(Name = "Number of Units")]
        //public int NumberOfUnits { get; set; }

        [Required(ErrorMessage = "The Contract Value is required.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Contract Value")]
        public decimal ContractValue { get; set; }

        //// Property to display the calculated value
        //[Display(Name = "Fee Per Unit")]
        //public decimal FeePerUnit { get; set; }

        public IEnumerable<SelectListItem>? Managers { get; set; } // Dropdown list of possible managers
    }
}
