using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

public class Payment : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = default!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }

    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentNotes { get; set; }
}
