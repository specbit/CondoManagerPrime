using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Condominium Staff")]
    public class StaffDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}