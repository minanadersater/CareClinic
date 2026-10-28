using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

/// <summary>
/// Doctor-specific profile data. Linked 1:1 to an Identity user via UserId.
/// The Domain layer intentionally does not reference ApplicationUser (which lives
/// in Infrastructure/Identity) to keep Domain free of framework dependencies.
/// </summary>
public class Doctor : BaseEntity
{
    public string UserId { get; set; } = default!; // FK -> AspNetUsers.Id

    public string FullName { get; set; } = default!;
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public string? ProfileImageUrl { get; set; }

    /// <summary>Default appointment slot duration in minutes, used to generate bookable slots.</summary>
    public int AppointmentDurationMinutes { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
    public ICollection<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
