using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents the view model for the home page, containing data to be displayed in the UI.
    /// </summary>
    public class HomeViewModel
    {
        public string CompanyName { get; set; }

        // This property to hold the list of companies for the dashboard
        public IEnumerable<Company> Companies { get; set; }

        // This property to hold the list of users for the dashboard
        public IEnumerable<ApplicationUserViewModel> AllUsers { get; set; }

        // Properties for Condominium Manager dashboard
        public bool IsManagerAssignedToCondominium { get; set; } = false;
        public string CondominiumName { get; set; }
        public string CondominiumAddress { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public List<ApplicationUser> CondominiumStaff { get; set; }

        public int UnitsCount { get; set; }
        public int CondominiumId { get; set; }
        // We'll add this later:
        // public int NumberOfOwners { get; set; }
    }
}
