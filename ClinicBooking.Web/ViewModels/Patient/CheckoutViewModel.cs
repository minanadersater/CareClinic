using System.ComponentModel.DataAnnotations;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Web.ViewModels.Patient;

public class CheckoutViewModel
{
    [Required]
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;
    public string ClinicAddress { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string Currency { get; set; } = "EGP";

    [Required]
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd

    [Required]
    public TimeSpan StartTime { get; set; }

    [Display(Name = "ملاحظات إضافية أو سبب الزيارة (اختياري)")]
    [MaxLength(500)]
    public string? PatientNotes { get; set; }

    [Required(ErrorMessage = "يرجى تحديد طريقة الدفع")]
    [Display(Name = "طريقة الدفع")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    // Card Details
    [Display(Name = "رقم البطاقة البنكية")]
    public string? CardNumber { get; set; }

    [Display(Name = "اسم حامل البطاقة")]
    public string? CardHolderName { get; set; }

    [Display(Name = "شهر الانتهاء")]
    public string? ExpiryMonth { get; set; }

    [Display(Name = "سنة الانتهاء")]
    public string? ExpiryYear { get; set; }

    [Display(Name = "رمز الأمان CVV")]
    public string? CVV { get; set; }

    // Mobile Wallet
    [Display(Name = "رقم المحفظة الإلكترونية (فودافون كاش / أورنج / اتصالات / وي)")]
    public string? WalletNumber { get; set; }

    // InstaPay
    [Display(Name = "عنوان الدفع اللحظي IPA أو رقم الهاتف على إنستاباي")]
    public string? InstaPayHandle { get; set; }
}
