using ClinicBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Policy = "RequireDoctor")]
public class DashboardController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;

    public DashboardController(IDoctorService doctorService, IAppointmentService appointmentService)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var doctor = await _doctorService.GetDoctorByUserIdAsync(userId);
        if (doctor is null) return NotFound("Doctor profile not found.");

        var appointments = await _appointmentService.GetAppointmentsForDoctorAsync(doctor.Id);
        ViewBag.Doctor = doctor;
        ViewBag.TotalAppointments = appointments.Count;
        ViewBag.PendingAppointments = appointments.Count(a => a.Status == ClinicBooking.Domain.Enums.AppointmentStatus.Pending);
        ViewBag.TodayAppointments = appointments.Count(a => a.AppointmentDate == DateOnly.FromDateTime(DateTime.Today));

        return View(appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).Take(5).ToList());
    }
}

