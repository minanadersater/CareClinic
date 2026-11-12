using System.Net;
using System.Net.Mail;
using ClinicBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClinicBooking.Infrastructure.Email;

/// <summary>
/// SMTP-backed sender, used when Email:SmtpHost is configured. Credentials are
/// supplied from configuration/user-secrets/environment variables only.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.SmtpUserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.SmtpUserName, _options.SmtpPassword)
        };

        await client.SendMailAsync(message, ct);
    }
}
