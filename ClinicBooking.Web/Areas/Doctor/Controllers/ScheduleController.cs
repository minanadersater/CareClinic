using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Web.ViewModels.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public class ScheduleController : Controller
{
    private readonly IScheduleService _scheduleService;
    private readonly IDoctorService _doctorService;

    public ScheduleController(IScheduleService scheduleService, IDoctorService doctorService)
    {
        _scheduleService = scheduleService;
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var doctor = await _doctorService.GetByUserIdAsync(userId!);
        if (doctor == null) return NotFound("Doctor profile not found.");

        var existingSchedules = await _scheduleService.GetDoctorSchedulesAsync(doctor.Id);
        
        var model = new ManageScheduleViewModel();
        
        // Initialize 7 days of the week
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            var existing = existingSchedules.FirstOrDefault(s => (DayOfWeek)s.DayOfWeek == day);
            if (existing != null)
            {
                model.Schedules.Add(new ScheduleItemViewModel
                {
                    DayOfWeek = day,
                    IsWorkingDay = true,
                    StartTime = existing.StartTime,
                    EndTime = existing.EndTime,
                    SlotDurationMinutes = doctor.AppointmentDurationMinutes
                });
            }
            else
            {
                model.Schedules.Add(new ScheduleItemViewModel
                {
                    DayOfWeek = day,
                    IsWorkingDay = false,
                    SlotDurationMinutes = 30
                });
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ManageScheduleViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var doctor = await _doctorService.GetByUserIdAsync(userId!);
        if (doctor == null) return NotFound("Doctor profile not found.");

        if (!ModelState.IsValid)
            return View(model);

        var schedulesToSave = new List<DoctorSchedule>();

        foreach (var item in model.Schedules)
        {
            if (item.IsWorkingDay)
            {
                if (item.StartTime == null || item.EndTime == null || item.StartTime >= item.EndTime)
                {
                    ModelState.AddModelError("", $"Invalid time range for {item.DayOfWeek}. Start time must be before End time.");
                    return View(model);
                }

                schedulesToSave.Add(new DoctorSchedule
                {
                    DayOfWeek = (ClinicBooking.Domain.Enums.WeekDay)item.DayOfWeek,
                    StartTime = item.StartTime.Value,
                    EndTime = item.EndTime.Value
                });
            }
        }

        await _scheduleService.UpdateDoctorScheduleAsync(doctor.Id, schedulesToSave);

        var activeSlot = model.Schedules.FirstOrDefault(s => s.IsWorkingDay && s.SlotDurationMinutes >= 10 && s.SlotDurationMinutes <= 120);
        if (activeSlot != null && activeSlot.SlotDurationMinutes != doctor.AppointmentDurationMinutes)
        {
            doctor.AppointmentDurationMinutes = activeSlot.SlotDurationMinutes;
            await _doctorService.UpdateProfileAsync(doctor);
        }

        TempData["SuccessMessage"] = "Your schedule has been updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}

