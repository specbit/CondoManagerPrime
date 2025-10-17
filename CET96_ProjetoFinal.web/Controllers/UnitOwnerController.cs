using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Unit Owner")]
    public class UnitOwnerController : Controller
    {
        private readonly IUnitRepository _unitRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationUserRepository _userRepository;

        public UnitOwnerController(
                    IUnitRepository unitRepository,
                    UserManager<ApplicationUser> userManager,
                    IApplicationUserRepository userRepository)
        {
            _unitRepository = unitRepository;
            _userManager = userManager;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Index()
        {
            var owner = await _userManager.GetUserAsync(User);
            if (owner == null) return Unauthorized();

            var assignedUnit = await _unitRepository.GetUnitByOwnerIdWithDetailsAsync(owner.Id);

            if (assignedUnit == null)
            {
                return View("NoUnitAssigned");
            }

            // --- LOGIC TO FIND THE MANAGER ---
            ApplicationUser manager = null;
            if (!string.IsNullOrEmpty(assignedUnit.Condominium.CondominiumManagerId))
            {
                // Use the user repository to find the manager by their ID.
                manager = await _userRepository.GetUserByIdAsync(assignedUnit.Condominium.CondominiumManagerId);
            }

            var model = new UnitOwnerDashboardViewModel
            {
                Unit = assignedUnit,
                Condominium = assignedUnit.Condominium,
                Manager = manager
            };

            return View(model);
        }
    }
}