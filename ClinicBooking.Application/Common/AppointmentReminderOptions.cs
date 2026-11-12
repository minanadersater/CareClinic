namespace ClinicBooking.Application.Common;

/// <summary>
/// Bound from the "AppointmentReminder" configuration section. Controls the
/// background reminder job that notifies patients about upcoming appointments.
/// </summary>
public class AppointmentReminderOptions
{
    public const string SectionName = "AppointmentReminder";

    /// <summary>When false the background reminder job does not run.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How far ahead of the appointment a reminder is sent. An appointment is
    /// "due" once its start time is at most this many hours away.
    /// </summary>
    public int HoursBeforeAppointment { get; set; } = 24;

    /// <summary>How often the background job looks for appointments that are due.</summary>
    public int CheckIntervalMinutes { get; set; } = 30;
}
