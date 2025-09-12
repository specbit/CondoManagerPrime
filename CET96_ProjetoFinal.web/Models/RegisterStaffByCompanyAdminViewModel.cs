using Microsoft.AspNetCore.Mvc.Rendering;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents the data required to register a staff member by a company administrator.
    /// </summary>
    /// <remarks>This view model extends <see cref="RegisterCondominiumStaffViewModel"/> and includes
    /// additional  properties specific to the company administrator's context, such as the list of condominiums  and
    /// the associated company ID.</remarks>
    public class RegisterStaffByCompanyAdminViewModel : RegisterCondominiumStaffViewModel
    {
        // This new property will hold the list of condominiums for the dropdown menu.
        public IEnumerable<SelectListItem> CondominiumsList { get; set; }

        public int CompanyId { get; set; }
    }
}
