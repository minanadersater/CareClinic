namespace ClinicBooking.Application.Common.Interfaces;

/// <summary>
/// Sends reminder messages for upcoming appointments. Kept separate from the
/// hosted background service so the reminder logic can be tested directly.
/// </summary>
public interface IAppointmentReminderService
{
    /// <summary>
    /// Finds appointments that are due for a reminder as of <paramref name="now"/>
    /// and have not been reminded yet, then notifies the patient by email and
    /// in-app notification.
    /// </summary>
    /// <returns>The number of reminders sent.</returns>
    Task<int> SendDueRemindersAsync(DateTime now, CancellationToken ct = default);
}
