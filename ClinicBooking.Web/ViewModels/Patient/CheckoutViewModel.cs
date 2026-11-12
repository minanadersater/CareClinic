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

}
