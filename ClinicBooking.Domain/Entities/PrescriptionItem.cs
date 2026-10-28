using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public class PrescriptionItem : BaseEntity
{
    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = default!;

    public string MedicineName { get; set; } = default!;
    public string Dosage { get; set; } = default!;
    public string Frequency { get; set; } = default!;
    public string Duration { get; set; } = default!;
    public string? Instructions { get; set; }
}
