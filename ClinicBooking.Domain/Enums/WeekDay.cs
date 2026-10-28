namespace ClinicBooking.Domain.Enums;

/// <summary>
/// Explicit day-of-week enum used for doctor working schedules.
/// Kept separate from System.DayOfWeek so the persisted integer values
/// are guaranteed stable regardless of BCL changes.
/// </summary>
public enum WeekDay
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}
