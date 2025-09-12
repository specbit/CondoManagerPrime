namespace CET96_ProjetoFinal.web.Models
{
    public class ManagersListViewModel
    {
        public IEnumerable<ApplicationUserViewModel> AllUsers { get; set; } = new List<ApplicationUserViewModel>();
    }
}
