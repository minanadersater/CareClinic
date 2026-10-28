using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

/// <summary>
/// A patient-doctor booking for a discrete time slot.
/// AppointmentDate + StartTime + DoctorId is protected by a unique database
/// index (see DoctorConfiguration/AppointmentConfiguration) in addition to
/// application-level availability checks, since app-level checks alone are
/// not sufficient to prevent double booking under concurrent requests.
/// </summary>
public class Appointment : BaseEntity
{
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = default!;

    public DateOnly AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public string? PatientNotes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Concurrency token. EF Core will use this as a rowversion so that two
    /// simultaneous status updates (e.g. cancel + confirm) on the same
    /// appointment cannot silently overwrite one another.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    // Navigation
    public MedicalRecord? MedicalRecord { get; set; }
    public Review? Review { get; set; }
    public Payment? Payment { get; set; }
}
