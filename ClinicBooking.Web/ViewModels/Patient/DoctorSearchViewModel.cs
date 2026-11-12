using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Web.ViewModels.Patient;

/// <summary>
/// Backs the "Find a Doctor" screen: the current filter values, the matching
/// doctors, and the specialties available for the dropdown.
/// </summary>
public class DoctorSearchViewModel
{
    public string? SearchTerm { get; set; }
    public int? SpecialtyId { get; set; }
    public decimal? MinFee { get; set; }
    public decimal? MaxFee { get; set; }
    public DoctorSortOrder Sort { get; set; } = DoctorSortOrder.NameAscending;

    public IReadOnlyList<DoctorDto> Doctors { get; set; } = new List<DoctorDto>();
    public IReadOnlyList<Specialty> Specialties { get; set; } = new List<Specialty>();

    /// <summary>True when a filter is active, used to tailor the empty-state message.</summary>
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) || SpecialtyId is > 0 || MinFee is not null || MaxFee is not null;
}
