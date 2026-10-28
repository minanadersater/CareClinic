using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public class Prescription : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = default!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    public int? MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }

    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
