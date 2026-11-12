namespace ClinicBooking.Application.Common;

/// <summary>
/// Business rules for when a patient may still change an appointment.
/// The cutoff is measured against the appointment's local start time so that
/// "2 hours before" means what the patient expects in their own time zone.
/// </summary>
public static class AppointmentPolicy
{
    public const int DefaultCancellationCutoffHours = 2;

    /// <summary>Combines the stored date and start time into a local <see cref="DateTime"/>.</summary>
    public static DateTime LocalStart(DateOnly date, TimeSpan startTime) =>
        date.ToDateTime(TimeOnly.FromTimeSpan(startTime));

    /// <summary>
    /// True when the appointment starts at least <paramref name="cutoffHours"/>
    /// from <paramref name="now"/>. A cutoff of zero means "up to the start time".
    /// </summary>
    public static bool IsWithinChangeWindow(DateOnly date, TimeSpan startTime, DateTime now, int cutoffHours)
    {
        if (cutoffHours < 0)
        {
            cutoffHours = 0;
        }

        return LocalStart(date, startTime) - now >= TimeSpan.FromHours(cutoffHours);
    }
}
