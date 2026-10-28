using ClinicBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdmin")]
public class AppointmentController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPaymentService _paymentService;

    public AppointmentController(IAppointmentService appointmentService, IPaymentService paymentService)
    {
        _appointmentService = appointmentService;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index()
    {
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        return View(appointments);
    }

    public async Task<IActionResult> Details(int id)
    {
        var appt = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appt is null) return NotFound();
        return View(appt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaymentPaid(int appointmentId, int paymentId)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
        if (appointment is null) return NotFound();

        await _paymentService.MarkAsPaidAsync(paymentId, "تم تحصيل المبلغ نقداً في العيادة (تسجيل الإدارة)");
        TempData["SuccessMessage"] = "تم تأكيد تحصيل المبلغ نقداً بنجاح.";
        return RedirectToAction(nameof(Details), new { id = appointmentId });
    }
}
