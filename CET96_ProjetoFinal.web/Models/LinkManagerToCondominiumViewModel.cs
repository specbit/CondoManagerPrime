using CET96_ProjetoFinal.web.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class LinkManagerToCondominiumViewModel
    {
        public string UserId { get; set; }

        public string FullName { get; set; }

        // dropdown binding target
        /// <summary>
        /// This property will hold the ID of the condominium selected from the dropdown.
        /// </summary>
        [Display(Name = "Assign to Condominium")]
        public int? SelectedCondominiumId { get; set; }

        // used to render the dropdown
        /// <summary>
        /// This holds the list of available condominiums to populate the dropdown.
        /// </summary>
        public IEnumerable<Condominium> CondominiumsList { get; set; }

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