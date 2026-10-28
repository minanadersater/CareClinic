using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

/// <summary>
/// A clinical record created by a doctor, normally after a completed appointment.
/// Access must be restricted server-side to the owning patient, the treating
/// doctor, and audited admin access only.
/// </summary>
public class MedicalRecord : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = default!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string? Diagnosis { get; set; }
    public string? Symptoms { get; set; }
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
