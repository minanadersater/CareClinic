namespace ClinicBooking.Domain.Entities;

/// <summary>
/// Explicit join entity for the Doctor &lt;-&gt; Specialty many-to-many relationship.
/// Modeled explicitly (rather than a pure skip-navigation) so it can carry
/// future metadata (e.g. IsPrimarySpecialty) without a migration to restructure it.
/// </summary>
public class DoctorSpecialty
{
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = default!;
}
