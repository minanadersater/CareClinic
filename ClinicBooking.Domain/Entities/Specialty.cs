using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public class Specialty : BaseEntity
{
    public string Name { get; set; } = default!; // must be unique
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
}
