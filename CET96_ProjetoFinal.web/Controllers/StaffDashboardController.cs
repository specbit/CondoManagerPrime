using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Condominium Staff")]
    public class StaffDashboardController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffDashboardController(
            IApplicationUserRepository userRepository,
            ICondominiumRepository condominiumRepository,
            UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _condominiumRepository = condominiumRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var staffMember = await _userManager.GetUserAsync(User);
            if (staffMember == null) return Unauthorized();

            // Check if the staff member is assigned to a condominium.
            if (!staffMember.CondominiumId.HasValue)
            {
                return View("NoCondominiumAssigned");
            }

            var condominium = await _condominiumRepository.GetByIdAsync(staffMember.CondominiumId.Value);
            if (condominium == null)
            {
                // This case handles if the condo was deleted but the staff member wasn't updated.
                return View("NoCondominiumAssigned");
            }

            ApplicationUser manager = null;
            if (!string.IsNullOrEmpty(condominium.CondominiumManagerId))
            {
                manager = await _userRepository.GetUserByIdAsync(condominium.CondominiumManagerId);
            }

            var model = new StaffDashboardViewModel
            {
                Condominium = condominium,
                Manager = manager
            };

            return View(model);
        }
    }
}