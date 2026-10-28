using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto?> GetPaymentByIdAsync(int id, CancellationToken ct = default);
    Task<PaymentDto?> GetPaymentByAppointmentIdAsync(int appointmentId, CancellationToken ct = default);
    Task<PaymentDto> CreatePaymentAsync(int appointmentId, decimal amount, PaymentMethod method, PaymentStatus status, string? transactionId = null, string? notes = null, CancellationToken ct = default);
    Task<PaymentDto> ProcessElectronicPaymentAsync(ProcessPaymentDto dto, CancellationToken ct = default);
    Task MarkAsPaidAsync(int paymentId, string? notes = null, CancellationToken ct = default);
    Task RefundPaymentAsync(int paymentId, string? reason = null, CancellationToken ct = default);
}
