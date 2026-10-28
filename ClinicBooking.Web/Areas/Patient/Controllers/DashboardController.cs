using ClinicBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Policy = "RequirePatient")]
public class DashboardController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;

    public DashboardController(IPatientService patientService, IAppointmentService appointmentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var patient = await _patientService.GetPatientByUserIdAsync(userId);
        if (patient is null) return NotFound("Patient profile not found.");

        var appointments = await _appointmentService.GetAppointmentsForPatientAsync(patient.Id);
        ViewBag.Patient = patient;
        ViewBag.TotalAppointments = appointments.Count;
        ViewBag.UpcomingAppointments = appointments.Count(a =>
            a.AppointmentDate >= DateOnly.FromDateTime(DateTime.Today) &&
            a.Status != ClinicBooking.Domain.Enums.AppointmentStatus.Cancelled);

        return View(appointments.OrderByDescending(a => a.AppointmentDate).Take(5).ToList());
    }
}

