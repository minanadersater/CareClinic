using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Services;
using ClinicBooking.Web.ViewModels.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Policy = "RequireDoctor")]
public class AppointmentController : Controller
{
    private const int DefaultPageSize = 10;

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

    public async Task<IActionResult> Index(
        DoctorAppointmentTab tab = DoctorAppointmentTab.All,
        string? search = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        DoctorAppointmentSortOrder sort = DoctorAppointmentSortOrder.DateDescending,
        int page = 1)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return NotFound();

        var filter = new DoctorAppointmentFilter
        {
            Tab = tab,
            SearchTerm = search,
            FromDate = fromDate,
            ToDate = toDate,
            Sort = sort,
            Page = page,
            PageSize = DefaultPageSize
        };

        var now = DateTime.Now;
        var appointments = await _appointmentService.SearchAppointmentsForDoctorAsync(doctor.Id, filter, now);
        var stats = await _appointmentService.GetAppointmentStatsForDoctorAsync(doctor.Id, now);

        return View(new DoctorAppointmentsViewModel
        {
            Filter = filter,
            Appointments = appointments,
            Stats = stats
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var appointment = await GetCurrentDoctorAppointmentAsync(id);
        if (appointment is null) return NotFound();
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var appointment = await GetCurrentDoctorAppointmentAsync(id);
        if (appointment is null) return NotFound();
        if (appointment.Status != ClinicBooking.Domain.Enums.AppointmentStatus.Pending)
            return Conflict("Only pending appointments can be confirmed.");

        await _appointmentService.ConfirmAppointmentAsync(id);
        TempData["Success"] = "Appointment confirmed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var appointment = await GetCurrentDoctorAppointmentAsync(id);
        if (appointment is null) return NotFound();
        if (appointment.Status != ClinicBooking.Domain.Enums.AppointmentStatus.Confirmed)
            return Conflict("Only confirmed appointments can be marked complete.");

        await _appointmentService.CompleteAppointmentAsync(id);
        TempData["Success"] = "Appointment marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(CancelAppointmentDto dto)
    {
        var appointment = await GetCurrentDoctorAppointmentAsync(dto.AppointmentId);
        if (appointment is null) return NotFound();
        if (appointment.Status is ClinicBooking.Domain.Enums.AppointmentStatus.Cancelled or ClinicBooking.Domain.Enums.AppointmentStatus.Completed)
            return Conflict("This appointment can no longer be cancelled.");

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
        if (appointment is null || appointment.DoctorId != doctor.Id || appointment.PaymentId != paymentId) return NotFound();
        if (appointment.PaymentMethod != ClinicBooking.Domain.Enums.PaymentMethod.Cash ||
            appointment.PaymentStatus != ClinicBooking.Domain.Enums.PaymentStatus.Pending ||
            appointment.Status == ClinicBooking.Domain.Enums.AppointmentStatus.Cancelled)
            return Conflict("Only unpaid cash appointments can be marked as paid.");

        await _paymentService.MarkAsPaidAsync(paymentId, "تم تحصيل المبلغ نقداً في العيادة بواسطة الطبيب");
        TempData["Success"] = "تم تأكيد تحصيل المبلغ نقداً بنجاح.";
        return RedirectToAction(nameof(Details), new { id = appointmentId });
    }

    private async Task<ClinicBooking.Application.Common.DTOs.DoctorDto?> GetCurrentDoctorAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _doctorService.GetDoctorByUserIdAsync(userId);
    }

    private async Task<AppointmentDto?> GetCurrentDoctorAppointmentAsync(int appointmentId)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return null;

        var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
        return appointment?.DoctorId == doctor.Id ? appointment : null;
    }
}

