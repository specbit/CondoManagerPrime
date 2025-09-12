namespace CET96_ProjetoFinal.web.Models
{
    public class CompanyUsersViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public List<ApplicationUserViewModel> Managers { get; set; } = new();
        public List<ApplicationUserViewModel> Staff { get; set; } = new();
    }
}
