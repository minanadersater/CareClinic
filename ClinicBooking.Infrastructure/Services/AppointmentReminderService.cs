using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicBooking.Infrastructure.Services;

/// <summary>
/// Creates and delivers appointment reminders. An appointment is due when it is
/// neither cancelled nor completed, its start time is in the future, it starts
/// within <see cref="AppointmentReminderOptions.HoursBeforeAppointment"/> hours,
/// and no reminder has been recorded for it yet.
///
/// Duplicate reminders are prevented without a schema change by checking for an
/// existing <see cref="NotificationType.AppointmentReminder"/> notification for
/// the same appointment and recipient.
/// </summary>
public class AppointmentReminderService : IAppointmentReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly AppointmentReminderOptions _options;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        ApplicationDbContext context,
        IEmailSender emailSender,
        IOptions<AppointmentReminderOptions> options,
        ILogger<AppointmentReminderService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> SendDueRemindersAsync(DateTime now, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return 0;
        }

        var windowEnd = now.AddHours(_options.HoursBeforeAppointment);
        var today = DateOnly.FromDateTime(now);

        // A generous SQL-side date bound keeps the candidate set small; the exact
        // "starts within the window" test is applied in memory below because
        // combining DateOnly + TimeSpan into a DateTime is not translatable.
        var lastCandidateDate = DateOnly.FromDateTime(windowEnd.AddDays(1));

        var candidates = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .Where(a => a.AppointmentDate >= today && a.AppointmentDate <= lastCandidateDate)
            .Where(a => !_context.Notifications.Any(n =>
                n.AppointmentId == a.Id &&
                n.UserId == a.Patient.UserId &&
                n.Type == NotificationType.AppointmentReminder))
            .ToListAsync(ct);

        var due = candidates
            .Where(a =>
            {
                var start = AppointmentPolicy.LocalStart(a.AppointmentDate, a.StartTime);
                return start > now && start <= windowEnd;
            })
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        var sent = 0;

        foreach (var appointment in due)
        {
            var start = AppointmentPolicy.LocalStart(appointment.AppointmentDate, appointment.StartTime);
            var doctorName = appointment.Doctor?.FullName ?? "your doctor";
            var title = "Appointment reminder";
            var message =
                $"Reminder: you have an appointment with Dr. {doctorName} on " +
                $"{start:dddd, dd MMMM yyyy} at {start:HH\\:mm}. " +
                "Please arrive a few minutes early.";

            try
            {
                await _emailSender.SendAsync(
                    appointment.Patient.Email,
                    $"Reminder: your appointment on {start:dddd, dd MMMM yyyy}",
                    $"<p>Hello {appointment.Patient.FullName},</p>" +
                    $"<p>This is a reminder of your upcoming appointment:</p>" +
                    $"<ul>" +
                    $"<li><strong>Doctor:</strong> Dr. {doctorName}</li>" +
                    $"<li><strong>Date:</strong> {start:dddd, dd MMMM yyyy}</li>" +
                    $"<li><strong>Time:</strong> {start:HH\\:mm}</li>" +
                    $"</ul>" +
                    $"<p>Please arrive a few minutes early. If you can no longer attend, " +
                    $"you can cancel or reschedule from your account.</p>",
                    ct);
            }
            catch (Exception ex)
            {
                // A mail failure must not stop the remaining reminders, and the
                // in-app notification is still created below so the patient sees it.
                _logger.LogError(ex,
                    "Failed to send appointment reminder email for appointment {AppointmentId}.", appointment.Id);
            }

            _context.Notifications.Add(new Notification
            {
                UserId = appointment.Patient.UserId,
                Type = NotificationType.AppointmentReminder,
                Title = title,
                Message = message,
                IsRead = false,
                AppointmentId = appointment.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            sent++;
        }

        if (sent > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return sent;
    }
}
