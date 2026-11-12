namespace ClinicBooking.Application.Common.Interfaces;

/// <summary>
/// Abstraction for sending transactional email (password reset, notifications).
/// Implementations live in Infrastructure so the Application layer stays free of
/// any specific transport.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
