using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Policy = "RequireDoctor")]
public class AppointmentController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;
    private readonly IPaymentService _paymentService;

    public AppointmentController(
        IDoctorService doctorService,
        IAppointmentService appointmentService,
        IPaymentService paymentService)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return NotFound();
        var appointments = await _appointmentService.GetAppointmentsForDoctorAsync(doctor.Id);
        return View(appointments);
    }

    public async Task<IActionResult> Details(int id)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return NotFound();
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment is null || appointment.DoctorId != doctor.Id) return NotFound();
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        await _appointmentService.ConfirmAppointmentAsync(id);
        TempData["Success"] = "Appointment confirmed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        await _appointmentService.CompleteAppointmentAsync(id);
        TempData["Success"] = "Appointment marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(CancelAppointmentDto dto)
    {
        await _appointmentService.CancelAppointmentAsync(dto);
        TempData["Success"] = "Appointment cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaymentPaid(int appointmentId, int paymentId)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return NotFound();

        var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
        if (appointment is null || appointment.DoctorId != doctor.Id) return NotFound();

        await _paymentService.MarkAsPaidAsync(paymentId, "تم تحصيل المبلغ نقداً في العيادة بواسطة الطبيب");
        TempData["Success"] = "تم تأكيد تحصيل المبلغ نقداً بنجاح.";
        return RedirectToAction(nameof(Details), new { id = appointmentId });
    }

    private async Task<ClinicBooking.Application.Common.DTOs.DoctorDto?> GetCurrentDoctorAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _doctorService.GetDoctorByUserIdAsync(userId);
    }
}

