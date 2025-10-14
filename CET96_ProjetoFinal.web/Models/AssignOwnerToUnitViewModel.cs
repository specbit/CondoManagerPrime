using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// ViewModel for the page where an Admin or Manager assigns a
    /// "Unit Owner" user to a specific Unit.
    /// </summary>
    public class AssignOwnerToUnitViewModel
    {
        // Details of the unit we are assigning an owner to.
        public int UnitId { get; set; }
        public string UnitNumber { get; set; }
        public string CondominiumName { get; set; }

        // The ID of the owner selected from the dropdown.
        [Required(ErrorMessage = "Please select an owner.")]            
        [Display(Name = "Select Owner")]
        public string SelectedOwnerId { get; set; }

        // The list of available owners to populate the dropdown.
        [ValidateNever]                                        // <-- prevents list validation
        public IEnumerable<SelectListItem> AvailableOwners { get; set; }
        = new List<SelectListItem>();                      // <-- never null
    }
}