using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Unit Owner")]
    public class UnitOwnerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}