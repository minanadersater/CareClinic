using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetAllAppointmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> GetAppointmentsForDoctorAsync(int doctorId, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> GetAppointmentsForPatientAsync(int patientId, CancellationToken ct = default);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<TimeSpan>> GetAvailableSlotsAsync(int doctorId, DateTime date);
    Task<Appointment> BookAppointmentAsync(int patientId, int doctorId, DateTime date, TimeSpan startTime, ClinicBooking.Domain.Enums.PaymentMethod paymentMethod = ClinicBooking.Domain.Enums.PaymentMethod.Cash, string? paymentNotes = null, string? transactionId = null, string? patientNotes = null);
    Task<AppointmentDto> BookAppointmentAsync(int patientId, CreateAppointmentDto dto, CancellationToken ct = default);
    Task ConfirmAppointmentAsync(int appointmentId, CancellationToken ct = default);
    Task CompleteAppointmentAsync(int appointmentId, CancellationToken ct = default);
    Task CancelAppointmentAsync(CancelAppointmentDto dto, CancellationToken ct = default);
}
