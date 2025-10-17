using CET96_ProjetoFinal.web.Data.Entities;
using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Models
{
    public class StaffDashboardViewModel
    {
        public Condominium Condominium { get; set; }
        public ApplicationUser Manager { get; set; }
    }
}