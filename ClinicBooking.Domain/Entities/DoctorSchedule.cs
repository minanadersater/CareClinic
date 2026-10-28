using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

/// <summary>
/// A recurring weekly working-hours block for a doctor.
/// Bookable slots are generated from these rows combined with
/// Doctor.AppointmentDurationMinutes.
/// </summary>
public class DoctorSchedule : BaseEntity
{
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    public WeekDay DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsActive { get; set; } = true;
}
