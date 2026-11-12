using ClinicBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClinicBooking.Infrastructure.Email;

/// <summary>
/// Development fallback used when no SMTP host is configured. It does not send
/// anything; it writes the full message to the log so flows such as password
/// reset can still be completed locally (the link appears in the console log).
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Email sender is not configured (Email:SmtpHost is empty). The following email was NOT sent.\nTo: {To}\nSubject: {Subject}\nBody: {Body}",
            to, subject, htmlBody);

        return Task.CompletedTask;
    }
}
