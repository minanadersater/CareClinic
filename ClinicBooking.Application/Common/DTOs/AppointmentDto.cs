using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Common.DTOs;

public class AppointmentDto
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = default!;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = default!;
    public DateOnly AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? PatientNotes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Payment fields
    public int? PaymentId { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string PaymentCurrency { get; set; } = "EGP";
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentNotes { get; set; }
}

public class CreateAppointmentDto
{
    public int DoctorId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string? PatientNotes { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? PaymentNotes { get; set; }
    public string? TransactionId { get; set; }
}

public class CancelAppointmentDto
{
    public int AppointmentId { get; set; }
    public string? CancellationReason { get; set; }
}

public class AvailableSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

