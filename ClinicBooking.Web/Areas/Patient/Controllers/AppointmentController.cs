using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Policy = "RequirePatient")]
public class AppointmentController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(IPatientService patientService, IAppointmentService appointmentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
    }

    public async Task<IActionResult> Index()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return NotFound();
        var appointments = await _appointmentService.GetAppointmentsForPatientAsync(patient.Id);
        return View(appointments.OrderByDescending(a => a.AppointmentDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var appt = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appt is null) return NotFound();
        return View(appt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(CancelAppointmentDto dto)
    {
        await _appointmentService.CancelAppointmentAsync(dto);
        TempData["Success"] = "Appointment cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PatientDto?> GetCurrentPatientAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _patientService.GetPatientByUserIdAsync(userId);
    }
}

