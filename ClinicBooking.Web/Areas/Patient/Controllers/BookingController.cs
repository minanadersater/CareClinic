using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Web.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Roles = "Patient")]
public class BookingController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly ISpecialtyService _specialtyService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IDoctorService doctorService,
        IAppointmentService appointmentService,
        IPatientService patientService,
        ISpecialtyService specialtyService,
        ILogger<BookingController> logger)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
        _patientService = patientService;
        _specialtyService = specialtyService;
        _logger = logger;
    }

    public async Task<IActionResult> SelectDoctor(
        string? search,
        int? specialtyId,
        decimal? minFee,
        decimal? maxFee,
        DoctorSortOrder sort = DoctorSortOrder.NameAscending)
    {
        var filter = new DoctorSearchFilter
        {
            SearchTerm = search,
            SpecialtyId = specialtyId,
            MinFee = minFee,
            MaxFee = maxFee,
            Sort = sort
        };

        var doctors = await _doctorService.SearchActiveDoctorsAsync(filter);
        var specialties = await _specialtyService.GetActiveAsync();

        var model = new DoctorSearchViewModel
        {
            SearchTerm = search,
            SpecialtyId = specialtyId,
            MinFee = minFee,
            MaxFee = maxFee,
            Sort = sort,
            Doctors = doctors,
            Specialties = specialties.ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> SelectSlot(int doctorId, DateTime? date)
    {
        var selectedDate = date ?? DateTime.Today.AddDays(1); // Default to tomorrow
        
        var doctor = await _doctorService.GetByIdAsync(doctorId);
        if (doctor == null || !doctor.IsActive) return NotFound();

        var slots = await _appointmentService.GetAvailableSlotsAsync(doctorId, selectedDate);

        ViewBag.DoctorId = doctorId;
        ViewBag.DoctorName = doctor.FullName;
        ViewBag.ConsultationFee = doctor.ConsultationFee;
        ViewBag.SelectedDate = selectedDate;
        ViewBag.Slots = slots;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int doctorId, string date, TimeSpan startTime)
    {
        var doctor = await _doctorService.GetByIdAsync(doctorId);
        if (doctor == null || !doctor.IsActive) return NotFound();

        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var appointmentDate))
        {
            return RedirectToAction(nameof(SelectSlot), new { doctorId });
        }

        var availableSlots = await _appointmentService.GetAvailableSlotsAsync(doctorId, appointmentDate);
        if (!availableSlots.Contains(startTime))
        {
            TempData["ErrorMessage"] = "هذا الموعد لم يعد متاحاً. اختر موعداً آخر.";
            return RedirectToAction(nameof(SelectSlot), new { doctorId, date = appointmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        }

        var specialtyStr = doctor.DoctorSpecialties != null && doctor.DoctorSpecialties.Any()
            ? string.Join(", ", doctor.DoctorSpecialties.Select(ds => ds.Specialty?.Name).Where(n => !string.IsNullOrEmpty(n)))
            : "طبيب ممارس عام";

        var viewModel = new CheckoutViewModel
        {
            DoctorId = doctorId,
            DoctorName = doctor.FullName,
            DoctorSpecialty = specialtyStr,
            ClinicAddress = doctor.ClinicAddress ?? "عيادة CareClinic",
            ConsultationFee = doctor.ConsultationFee,
            Currency = "EGP",
            Date = appointmentDate.ToString("yyyy-MM-dd"),
            StartTime = startTime,
            PaymentMethod = PaymentMethod.Cash
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var doctor = await _doctorService.GetByIdAsync(model.DoctorId);
        if (doctor == null || !doctor.IsActive) return NotFound();

        if (!DateTime.TryParseExact(model.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var appointmentDate))
        {
            TempData["ErrorMessage"] = "تاريخ الموعد غير صالح. اختر التاريخ من جديد.";
            return RedirectToAction(nameof(SelectSlot), new { doctorId = model.DoctorId });
        }

        // No payment gateway is configured. Reject tampered requests instead
        // of recording an electronic payment as if it had succeeded.
        if (model.PaymentMethod != PaymentMethod.Cash)
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "الدفع الإلكتروني غير متاح حالياً. اختر الدفع نقداً في العيادة.");
        }

        const string paymentNotes = "الدفع نقداً في العيادة عند الحضور";

        if (!ModelState.IsValid)
        {
            // Reload doctor info for view display
            model.DoctorName = doctor.FullName;
            model.ConsultationFee = doctor.ConsultationFee;
            model.ClinicAddress = doctor.ClinicAddress ?? "عيادة CareClinic";
            model.DoctorSpecialty = doctor.DoctorSpecialties != null && doctor.DoctorSpecialties.Any()
                ? string.Join(", ", doctor.DoctorSpecialties.Select(ds => ds.Specialty?.Name).Where(n => !string.IsNullOrEmpty(n)))
                : "طبيب ممارس عام";

            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _patientService.GetPatientByUserIdAsync(userId!);
        if (patient == null) return NotFound("Patient profile not found.");

        try
        {
            var appointment = await _appointmentService.BookAppointmentAsync(
                patient.Id,
                model.DoctorId,
                appointmentDate,
                model.StartTime,
                PaymentMethod.Cash,
                paymentNotes,
                null,
                model.PatientNotes);

            TempData["SuccessMessage"] = "تم حجز موعدك بنجاح! يرجى سداد المبلغ نقداً عند حضورك للعيادة.";

            return RedirectToAction("Details", "Appointment", new { area = "Patient", id = appointment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment booking failed for patient {PatientId} and doctor {DoctorId}.", patient.Id, model.DoctorId);
            TempData["ErrorMessage"] = "تعذر تأكيد الحجز. قد يكون الموعد قد حُجز للتو؛ اختر موعداً آخر وحاول مجدداً.";
            return RedirectToAction(nameof(SelectSlot), new { doctorId = model.DoctorId, date = model.Date });
        }
    }
}
