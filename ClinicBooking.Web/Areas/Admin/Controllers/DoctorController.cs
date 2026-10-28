using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DoctorController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly ISpecialtyService _specialtyService;

    public DoctorController(IDoctorService doctorService, ISpecialtyService specialtyService)
    {
        _doctorService = doctorService;
        _specialtyService = specialtyService;
    }

    public async Task<IActionResult> Index()
    {
        var doctors = await _doctorService.GetAllAsync();
        
        var model = doctors.Select(d => new DoctorListViewModel
        {
            Id = d.Id,
            FullName = d.FullName,
            Email = d.Email,
            PhoneNumber = d.PhoneNumber,
            IsActive = d.IsActive,
            ConsultationFee = d.ConsultationFee,
            Specialties = d.DoctorSpecialties?.Select(ds => ds.Specialty?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>()
        });

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var specialties = await _specialtyService.GetActiveAsync();
        ViewBag.Specialties = new SelectList(specialties, "Id", "Name");
        return View(new CreateDoctorDto { AppointmentDurationMinutes = 30 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDoctorDto dto)
    {
        if (!ModelState.IsValid)
        {
            var specialties = await _specialtyService.GetActiveAsync();
            ViewBag.Specialties = new SelectList(specialties, "Id", "Name", dto.SpecialtyIds);
            return View(dto);
        }

        try
        {
            await _doctorService.CreateDoctorAsync(dto);
            TempData["SuccessMessage"] = $"Dr. {dto.FullName} was created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var specialties = await _specialtyService.GetActiveAsync();
            ViewBag.Specialties = new SelectList(specialties, "Id", "Name", dto.SpecialtyIds);
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);
        if (doctor == null) return NotFound();

        var specialties = await _specialtyService.GetAllAsync();
        var selectedIds = doctor.DoctorSpecialties?.Select(ds => ds.SpecialtyId).ToList() ?? new List<int>();
        ViewBag.Specialties = new SelectList(specialties, "Id", "Name", selectedIds);

        var dto = new UpdateDoctorDto
        {
            Id = doctor.Id,
            FullName = doctor.FullName,
            PhoneNumber = doctor.PhoneNumber,
            Bio = doctor.Bio,
            ConsultationFee = doctor.ConsultationFee,
            ClinicName = doctor.ClinicName,
            ClinicAddress = doctor.ClinicAddress,
            AppointmentDurationMinutes = doctor.AppointmentDurationMinutes,
            IsActive = doctor.IsActive,
            SpecialtyIds = selectedIds
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateDoctorDto dto)
    {
        if (id != dto.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = new SelectList(specialties, "Id", "Name", dto.SpecialtyIds);
            return View(dto);
        }

        try
        {
            await _doctorService.UpdateDoctorAsync(dto);
            TempData["SuccessMessage"] = "Doctor details updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var specialties = await _specialtyService.GetAllAsync();
            ViewBag.Specialties = new SelectList(specialties, "Id", "Name", dto.SpecialtyIds);
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await _doctorService.ToggleActiveStatusAsync(id);
        TempData["SuccessMessage"] = "Doctor status updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
