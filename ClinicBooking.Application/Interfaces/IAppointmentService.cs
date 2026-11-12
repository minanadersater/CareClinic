using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetAllAppointmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> GetAppointmentsForDoctorAsync(int doctorId, CancellationToken ct = default);

    /// <summary>
    /// Returns one page of a doctor's appointments after applying the tab, text
    /// search and date-range filters in <paramref name="filter"/>. Scope tabs
    /// (Today/Upcoming/Past) are evaluated against <paramref name="now"/>.
    /// </summary>
    Task<PagedResult<AppointmentDto>> SearchAppointmentsForDoctorAsync(
        int doctorId,
        DoctorAppointmentFilter filter,
        DateTime now,
        CancellationToken ct = default);

    /// <summary>Headline counts and payment totals for a doctor's appointments.</summary>
    Task<DoctorAppointmentStats> GetAppointmentStatsForDoctorAsync(
        int doctorId,
        DateTime now,
        CancellationToken ct = default);

    Task<IReadOnlyList<AppointmentDto>> GetAppointmentsForPatientAsync(int patientId, CancellationToken ct = default);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<TimeSpan>> GetAvailableSlotsAsync(int doctorId, DateTime date);
    Task<Appointment> BookAppointmentAsync(int patientId, int doctorId, DateTime date, TimeSpan startTime, ClinicBooking.Domain.Enums.PaymentMethod paymentMethod = ClinicBooking.Domain.Enums.PaymentMethod.Cash, string? paymentNotes = null, string? transactionId = null, string? patientNotes = null);
    Task<AppointmentDto> BookAppointmentAsync(int patientId, CreateAppointmentDto dto, CancellationToken ct = default);
    Task ConfirmAppointmentAsync(int appointmentId, CancellationToken ct = default);
    Task CompleteAppointmentAsync(int appointmentId, CancellationToken ct = default);
    Task CancelAppointmentAsync(CancelAppointmentDto dto, CancellationToken ct = default);

    /// <summary>
    /// True when a patient may still cancel or reschedule the appointment: it is
    /// neither cancelled nor completed, and it starts at least
    /// <paramref name="cutoffHours"/> hours from now.
    /// </summary>
    Task<bool> CanPatientModifyAsync(int appointmentId, int cutoffHours, CancellationToken ct = default);

    /// <summary>
    /// Patient-initiated cancellation that enforces the cutoff window. Staff
    /// cancellation should use <see cref="CancelAppointmentAsync"/> instead.
    /// </summary>
    Task CancelByPatientAsync(CancelAppointmentDto dto, int cutoffHours, CancellationToken ct = default);

    /// <summary>
    /// Moves an appointment to a new slot for the same doctor, keeping the same
    /// payment. Enforces the cutoff window and re-runs all availability checks.
    /// The appointment is reset to Pending so the clinic re-confirms it.
    /// </summary>
    Task<AppointmentDto> RescheduleAppointmentAsync(RescheduleAppointmentDto dto, int cutoffHours, CancellationToken ct = default);
}
