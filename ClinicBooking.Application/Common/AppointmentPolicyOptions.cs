namespace ClinicBooking.Application.Common;

/// <summary>
/// Bound from the "AppointmentPolicy" configuration section. Controls how close
/// to the appointment a patient may still cancel or reschedule it.
/// </summary>
public class AppointmentPolicyOptions
{
    public const string SectionName = "AppointmentPolicy";

    /// <summary>
    /// Minimum number of hours before the appointment during which a patient can
    /// still cancel or reschedule. Defaults to <see cref="AppointmentPolicy.DefaultCancellationCutoffHours"/>.
    /// </summary>
    public int CancellationCutoffHours { get; set; } = AppointmentPolicy.DefaultCancellationCutoffHours;
}
