using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

/// <summary>
/// A patient review of a doctor. Eligibility (completed appointment, one
/// review per appointment) is enforced at the service layer and reinforced
/// by a unique index on AppointmentId.
/// </summary>
public class Review : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = default!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = default!;

    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public bool IsHidden { get; set; } = false; // admin moderation
}
