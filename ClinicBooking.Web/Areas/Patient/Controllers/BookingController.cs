using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Web.ViewModels.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Roles = "Patient")]
public class BookingController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;

    public BookingController(
        IDoctorService doctorService,
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
        _patientService = patientService;
    }

    public async Task<IActionResult> SelectDoctor()
    {
        var doctors = await _doctorService.GetActiveAsync();
        return View(doctors);
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

        if (!DateTime.TryParse(date, out var appointmentDate))
        {
            return RedirectToAction(nameof(SelectSlot), new { doctorId });
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

        if (!DateTime.TryParse(model.Date, out var appointmentDate))
        {
            ModelState.AddModelError("", "تاريخ الموعد غير صالح.");
        }

        // Validate electronic payment fields based on selected method
        string? paymentNotes = null;
        string? txnId = null;

        if (model.PaymentMethod == PaymentMethod.CreditCard)
        {
            if (string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Replace(" ", "").Length < 16)
                ModelState.AddModelError(nameof(model.CardNumber), "يرجى إدخال رقم بطاقة صالح يتكون من 16 رقماً.");
            if (string.IsNullOrWhiteSpace(model.CardHolderName))
                ModelState.AddModelError(nameof(model.CardHolderName), "يرجى إدخال اسم حامل البطاقة كما هو مدون عليها.");
            if (string.IsNullOrWhiteSpace(model.CVV) || model.CVV.Length < 3)
                ModelState.AddModelError(nameof(model.CVV), "رمز الأمان CVV مطلوب (3 أو 4 أرقام).");

            if (ModelState.IsValid)
            {
                var cleanCard = model.CardNumber!.Replace(" ", "");
                var masked = $"**** **** **** {cleanCard[^4..]}";
                txnId = $"TXN-CARD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                paymentNotes = $"دفع إلكتروني بواسطة بطاقة بنكية ({masked}) - صاحب البطاقة: {model.CardHolderName}";
            }
        }
        else if (model.PaymentMethod == PaymentMethod.MobileWallet)
        {
            if (string.IsNullOrWhiteSpace(model.WalletNumber) || !System.Text.RegularExpressions.Regex.IsMatch(model.WalletNumber, @"^01[0125][0-9]{8}$"))
                ModelState.AddModelError(nameof(model.WalletNumber), "يرجى إدخال رقم هاتف محفظة إلكترونية مصري صحيح (مثال: 01012345678).");

            if (ModelState.IsValid)
            {
                txnId = $"TXN-WLT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                paymentNotes = $"دفع إلكتروني عبر محفظة كاش برقم: {model.WalletNumber}";
            }
        }
        else if (model.PaymentMethod == PaymentMethod.InstaPay)
        {
            if (string.IsNullOrWhiteSpace(model.InstaPayHandle))
                ModelState.AddModelError(nameof(model.InstaPayHandle), "يرجى إدخال عنوان إنستاباي IPA أو رقم الهاتف المسجل.");

            if (ModelState.IsValid)
            {
                txnId = $"TXN-INSTA-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                paymentNotes = $"دفع إلكتروني فوري عبر إنستاباي InstaPay ({model.InstaPayHandle})";
            }
        }
        else // Cash
        {
            paymentNotes = "الدفع نقداً في العيادة عند الحضور";
        }

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
                model.PaymentMethod,
                paymentNotes,
                txnId,
                model.PatientNotes);

            TempData["SuccessMessage"] = model.PaymentMethod == PaymentMethod.Cash
                ? "تم حجز موعدك بنجاح! يرجى سداد المبلغ نقداً عند حضورك للعيادة."
                : $"تم الدفع بنجاح ({doctor.ConsultationFee} ج.م) وتأكيد موعدك! رقم العملية: {txnId}";

            return RedirectToAction("Details", "Appointment", new { area = "Patient", id = appointment.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(SelectSlot), new { doctorId = model.DoctorId, date = model.Date });
        }
    }
}
