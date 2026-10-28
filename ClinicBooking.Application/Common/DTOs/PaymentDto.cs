using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Common.DTOs;

public class PaymentDto
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentNotes { get; set; }
}

public class ProcessPaymentDto
{
    public int AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }

    // Card Details
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? CVV { get; set; }

    // Mobile Wallet
    public string? WalletNumber { get; set; }

    // InstaPay
    public string? InstaPayHandle { get; set; }
}
