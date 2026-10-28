using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SpecialtyController : Controller
{
    private readonly ISpecialtyService _specialtyService;

    public SpecialtyController(ISpecialtyService specialtyService)
    {
        _specialtyService = specialtyService;
    }

    public async Task<IActionResult> Index()
    {
        var specialties = await _specialtyService.GetAllAsync();
        return View(specialties);
    }

    public IActionResult Create()
    {
        return View(new SpecialtyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SpecialtyViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _specialtyService.ExistsAsync(model.Name))
        {
            ModelState.AddModelError("Name", "A specialty with this exact name already exists.");
            return View(model);
        }

        var specialty = new Specialty
        {
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive
        };

        await _specialtyService.AddAsync(specialty);
        TempData["SuccessMessage"] = "Specialty created successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var specialty = await _specialtyService.GetByIdAsync(id);
        if (specialty == null)
            return NotFound();

        var model = new SpecialtyViewModel
        {
            Id = specialty.Id,
            Name = specialty.Name,
            Description = specialty.Description,
            IsActive = specialty.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SpecialtyViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        if (await _specialtyService.ExistsAsync(model.Name, model.Id))
        {
            ModelState.AddModelError("Name", "Another specialty with this name already exists.");
            return View(model);
        }

        var specialty = await _specialtyService.GetByIdAsync(id);
        if (specialty == null)
            return NotFound();

        specialty.Name = model.Name;
        specialty.Description = model.Description;
        specialty.IsActive = model.IsActive;

        await _specialtyService.UpdateAsync(specialty);
        TempData["SuccessMessage"] = "Specialty updated successfully!";
        return RedirectToAction(nameof(Index));
    }
}

