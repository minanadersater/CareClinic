namespace ClinicBooking.Domain.Enums;

/// <summary>
/// Lifecycle of an appointment.
/// Pending -> Confirmed -> Completed (normal flow)
/// Pending -> Cancelled, Confirmed -> Cancelled (early exit)
/// Cancelled and Completed are terminal states.
/// </summary>
public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}
