using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Form model for creating a Condominium Staff user, usable by both
    /// Company Administrators and Condominium Managers.
    /// Inherits all staff fields from <see cref="RegisterCondominiumStaffViewModel"/>.
    /// </summary>
    public class RegisterStaffFormViewModel : RegisterCondominiumStaffViewModel
    {
        /// <summary>
        /// Company context for the staff member being created.
        /// </summary>
        public int CompanyId { get; set; }

        /// <summary>
        /// List of condominiums available to the current caller (used to populate a dropdown).
        /// For Company Admins this typically includes all active condos in the company.
        /// For Managers this will usually be a single item (their assigned condo).
        /// </summary>
        public IEnumerable<SelectListItem> CondominiumsList { get; set; }
            = System.Linq.Enumerable.Empty<SelectListItem>();

        /// <summary>
        /// If true, the UI should allow choosing a condominium from <see cref="CondominiumsList"/>.
        /// If false, the UI should lock the selection (manager path) and display <see cref="SelectedCondominiumName"/>.
        /// </summary>
        public bool CanPickCondominium { get; set; } = true;

        /// <summary>
        /// Optional, display-only name for the condominium when the selection is locked (manager path).
        /// </summary>
        public string? SelectedCondominiumName { get; set; }
    }
}
