using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClinicBooking.Web.Services;

/// <summary>
/// Periodically asks <see cref="IAppointmentReminderService"/> to send reminders
/// for upcoming appointments. The reminder rules live in the service; this class
/// only provides the timer and a scoped service boundary per tick.
/// </summary>
public class AppointmentReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppointmentReminderOptions _options;
    private readonly ILogger<AppointmentReminderBackgroundService> _logger;

    public AppointmentReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppointmentReminderOptions> options,
        ILogger<AppointmentReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Appointment reminders are disabled (AppointmentReminder:Enabled = false).");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.CheckIntervalMinutes));
        _logger.LogInformation(
            "Appointment reminder service started: every {Minutes} minute(s), {Hours} hour(s) before the appointment.",
            interval.TotalMinutes, _options.HoursBeforeAppointment);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IAppointmentReminderService>();
                var sent = await reminders.SendDueRemindersAsync(DateTime.Now, stoppingToken);

                if (sent > 0)
                {
                    _logger.LogInformation("Sent {Count} appointment reminder(s).", sent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a failed tick kill the background loop.
                _logger.LogError(ex, "Sending appointment reminders failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
