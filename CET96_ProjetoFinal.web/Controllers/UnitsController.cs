using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    public class UnitsController : Controller
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ICondominiumRepository _condominiumRepository;

        public UnitsController(IUnitRepository unitRepository, ICondominiumRepository condominiumRepository)
        {
            _unitRepository = unitRepository;
            _condominiumRepository = condominiumRepository;
        }

        // GET: Units?condominiumId=5
        public async Task<IActionResult> Index(int condominiumId)
        {
            var units = await _unitRepository.GetUnitsByCondominiumIdAsync(condominiumId);
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);

            ViewBag.CondominiumId = condominiumId;
            ViewBag.CondominiumName = condominium?.Name;

            return View(units);
        }

        // GET: Units/Create?condominiumId=5
        public IActionResult Create(int condominiumId)
        {
            var model = new CreateUnitViewModel
            {
                CondominiumId = condominiumId
            };
            return View(model);
        }

        // POST: Units/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUnitViewModel model)
        {
            if (ModelState.IsValid)
            {
                var unit = new Unit
                {
                    UnitNumber = model.UnitNumber,
                    CondominiumId = model.CondominiumId
                };

                await _unitRepository.CreateAsync(unit);
                await _unitRepository.SaveAllAsync();

                TempData["StatusMessage"] = "Unit created successfully.";
                return RedirectToAction(nameof(Index), new { condominiumId = model.CondominiumId });
            }

            return View(model);
        }
    }
}