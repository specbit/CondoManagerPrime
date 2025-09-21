using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class AssignmentViewModel
    {
        public string UserId { get; set; }

        public string? FullName { get; set; }

        // dropdown binding target
        /// <summary>
        /// Gets or sets the ID of the condominium selected from the dropdown.
        /// This is the binding target for the form submission.
        /// </summary>
        [Display(Name = "Assign to Condominium")]
        // We use [Range(1, int.MaxValue)] instead of [Required] for dropdowns.
        // This ensures the user selects a real condominium (with an ID of 1 or higher)
        // and not the default "-- Select Condominium --" option, which has a value of 0 or null.
        [Range(1, int.MaxValue, ErrorMessage = "You must select a condominium to assign.")]
        public int? SelectedCondominiumId { get; set; }

        // used to render the dropdown
        /// <summary>
        /// This holds the list of available condominiums to populate the dropdown.
        /// </summary>
        public IEnumerable<SelectListItem>? CondominiumsList { get; set; }

        // informational
        /// <summary>
        /// This will display the name of the condominium the manager is currently assigned to, if any.
        /// </summary>
        [Display(Name = "Currently Assigned To")]
        public string? CurrentlyAssignedCondominiumName { get; set; }

        // for navigation back to list
        public int CompanyId { get; set; }
    }
}