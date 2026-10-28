using AutoMapper;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class PaymentService : IPaymentService, ClinicBooking.Application.Services.IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public PaymentService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(int id, CancellationToken ct = default)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
    }

    public async Task<PaymentDto?> GetPaymentByAppointmentIdAsync(int appointmentId, CancellationToken ct = default)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);

        return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
    }

    public async Task<PaymentDto> CreatePaymentAsync(
        int appointmentId,
        decimal amount,
        PaymentMethod method,
        PaymentStatus status,
        string? transactionId = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        var existing = await _context.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);
        if (existing != null)
        {
            existing.Amount = amount;
            existing.Method = method;
            existing.Status = status;
            existing.TransactionId = transactionId ?? existing.TransactionId;
            existing.PaymentNotes = notes ?? existing.PaymentNotes;
            if (status == PaymentStatus.Paid && !existing.PaidAt.HasValue)
            {
                existing.PaidAt = DateTime.UtcNow;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return _mapper.Map<PaymentDto>(existing);
        }

        var payment = new Payment
        {
            AppointmentId = appointmentId,
            Amount = amount,
            Currency = "EGP",
            Method = method,
            Status = status,
            TransactionId = transactionId,
            PaidAt = status == PaymentStatus.Paid ? DateTime.UtcNow : null,
            PaymentNotes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaymentDto> ProcessElectronicPaymentAsync(ProcessPaymentDto dto, CancellationToken ct = default)
    {
        // Simulated Payment Gateway processing (Visa/Mastercard/Meeza, Mobile Wallets, InstaPay)
        string txnId = $"TXN-EGP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        string paymentDetails;

        switch (dto.Method)
        {
            case PaymentMethod.CreditCard:
                var maskedCard = !string.IsNullOrWhiteSpace(dto.CardNumber) && dto.CardNumber.Length >= 4
                    ? $"**** **** **** {dto.CardNumber[^4..]}"
                    : "بطاقة دفع إلكتروني";
                paymentDetails = $"الدفع بواسطة بطاقة بنكية ({maskedCard}) - صاحب البطاقة: {dto.CardHolderName}";
                break;

            case PaymentMethod.MobileWallet:
                paymentDetails = $"الدفع عبر المحفظة الإلكترونية ({dto.WalletNumber})";
                break;

            case PaymentMethod.InstaPay:
                paymentDetails = $"الدفع عبر إنستاباي InstaPay ({dto.InstaPayHandle})";
                break;

            default:
                paymentDetails = "دفع إلكتروني";
                break;
        }

        return await CreatePaymentAsync(
            dto.AppointmentId,
            dto.Amount,
            dto.Method,
            PaymentStatus.Paid,
            txnId,
            paymentDetails,
            ct);
    }

    public async Task MarkAsPaidAsync(int paymentId, string? notes = null, CancellationToken ct = default)
    {
        var payment = await _context.Payments.FindAsync(new object[] { paymentId }, ct)
            ?? throw new InvalidOperationException("Payment record not found.");

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            payment.PaymentNotes = string.IsNullOrWhiteSpace(payment.PaymentNotes)
                ? notes
                : $"{payment.PaymentNotes} | {notes}";
        }
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    public async Task RefundPaymentAsync(int paymentId, string? reason = null, CancellationToken ct = default)
    {
        var payment = await _context.Payments.FindAsync(new object[] { paymentId }, ct)
            ?? throw new InvalidOperationException("Payment record not found.");

        payment.Status = PaymentStatus.Refunded;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            payment.PaymentNotes = string.IsNullOrWhiteSpace(payment.PaymentNotes)
                ? $"مسترد: {reason}"
                : $"{payment.PaymentNotes} | مسترد: {reason}";
        }
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}
