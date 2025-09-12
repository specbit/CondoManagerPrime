namespace CET96_ProjetoFinal.web.Models
{
    public class StaffListViewModel
    {
        public IEnumerable<ApplicationUserViewModel> AllUsers { get; set; } = new List<ApplicationUserViewModel>();
    }
}
