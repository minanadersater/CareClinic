using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Web.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Policy = "RequirePatient")]
public class AppointmentController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly AppointmentPolicyOptions _policy;

    public AppointmentController(
        IPatientService patientService,
        IAppointmentService appointmentService,
        IDoctorService doctorService,
        IOptions<AppointmentPolicyOptions> policy)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _policy = policy.Value;
    }

    public async Task<IActionResult> Index()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return NotFound();
        var appointments = await _appointmentService.GetAppointmentsForPatientAsync(patient.Id);
        ViewBag.CancellationCutoffHours = _policy.CancellationCutoffHours;
        return View(appointments.OrderByDescending(a => a.AppointmentDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return NotFound();

        var appt = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appt is null || appt.PatientId != patient.Id) return NotFound();

        ViewBag.CancellationCutoffHours = _policy.CancellationCutoffHours;
        return View(appt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(CancelAppointmentDto dto)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return NotFound();

        var appointment = await _appointmentService.GetAppointmentByIdAsync(dto.AppointmentId);
        if (appointment is null || appointment.PatientId != patient.Id) return NotFound();

        try
        {
            await _appointmentService.CancelByPatientAsync(dto, _policy.CancellationCutoffHours);
            TempData["Success"] = "Appointment cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int id, DateTime? date)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return NotFound();

        var appt = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appt is null || appt.PatientId != patient.Id) return NotFound();

        if (!await _appointmentService.CanPatientModifyAsync(id, _policy.CancellationCutoffHours))
        {
            TempData["Error"] =
                $"This appointment can no longer be changed online. Appointments can be rescheduled at least {_policy.CancellationCutoffHours} hour(s) before they start.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var doctor = await _doctorService.GetByIdAsync(appt.DoctorId);
        if (doctor is null || !doctor.IsActive) return NotFound();

        var currentDate = appt.AppointmentDate.ToDateTime(TimeOnly.MinValue);
        var selectedDate = date
            ?? (currentDate.Date > DateTime.Today ? currentDate.Date : DateTime.Today.AddDays(1));

        var slots = await _appointmentService.GetAvailableSlotsAsync(appt.DoctorId, selectedDate);

        var model = new RescheduleAppointmentViewModel
        {
            AppointmentId = appt.Id,
            DoctorId = appt.DoctorId,
            DoctorName = appt.DoctorName,
            CurrentDate = appt.AppointmentDate,
            CurrentStartTime = appt.StartTime,
            SelectedDate = selectedDate,
            Slots = slots.ToList(),
            CancellationCutoffHours = _policy.CancellationCutoffHours
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int appointmentId, DateTime date, TimeSpan startTime)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return NotFound();

        var appt = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
        if (appt is null || appt.PatientId != patient.Id) return NotFound();

        try
        {
            await _appointmentService.RescheduleAppointmentAsync(new RescheduleAppointmentDto
            {
                AppointmentId = appointmentId,
                AppointmentDate = DateOnly.FromDateTime(date),
                StartTime = startTime,
                Reason = "Rescheduled by patient"
            }, _policy.CancellationCutoffHours);

            TempData["Success"] = "Appointment rescheduled. The clinic will confirm the new time.";
            return RedirectToAction(nameof(Details), new { id = appointmentId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Reschedule), new { id = appointmentId, date = date.ToString("yyyy-MM-dd") });
        }
    }

    private async Task<PatientDto?> GetCurrentPatientAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _patientService.GetPatientByUserIdAsync(userId);
    }
}
