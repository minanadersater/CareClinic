namespace ClinicBooking.Infrastructure.Email;

/// <summary>
/// Bound from the "Email" configuration section. When <see cref="SmtpHost"/> is
/// empty the application falls back to a logging sender (development only).
/// </summary>
public class EmailOptions
{
    public string FromAddress { get; set; } = "no-reply@clinicbooking.local";
    public string FromName { get; set; } = "CareClinic";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUserName { get; set; }
    public string? SmtpPassword { get; set; }
    public bool EnableSsl { get; set; } = true;
}
