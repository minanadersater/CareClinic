using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

/// <summary>
/// Patient-specific profile data. Linked 1:1 to an Identity user via UserId.
/// </summary>
public class Patient : BaseEntity
{
    public string UserId { get; set; } = default!; // FK -> AspNetUsers.Id

    public string FullName { get; set; } = default!;
    public Gender Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
