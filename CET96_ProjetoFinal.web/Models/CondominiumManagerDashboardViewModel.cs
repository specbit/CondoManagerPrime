using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Models
{
    public class CondominiumManagerDashboardViewModel
    {
        /// <summary>
        /// Represents the condominium managed by the logged-in user.
        /// </summary>
        public Condominium Condominium { get; set; }

        /// <summary>
        /// A collection of staff members working at the managed condominium.
        /// </summary>
        public IEnumerable<ApplicationUser> Staff { get; set; }
    }
}