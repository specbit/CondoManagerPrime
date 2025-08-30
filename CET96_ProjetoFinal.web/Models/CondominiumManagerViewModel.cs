using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents the view model for the home page, containing data to be displayed in the UI.
    /// </summary>
    public class CondominiumManagerViewModel
    {
        public string CompanyName { get; set; }

        // This property to hold the list of companies for the dashboard
        public IEnumerable<Company> Companies { get; set; }

        // This property to hold the list of users for the dashboard
        public IEnumerable<ApplicationUserViewModel> AllUsers { get; set; }
    }
}
