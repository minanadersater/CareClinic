namespace ClinicBooking.Application.Common.DTOs;

/// <summary>Ordering options offered on the doctor search screen.</summary>
public enum DoctorSortOrder
{
    NameAscending = 0,
    FeeAscending = 1,
    FeeDescending = 2
}

/// <summary>
/// Criteria used to narrow the list of active doctors shown to patients.
/// Every field is optional; an empty filter returns all active doctors.
/// </summary>
public class DoctorSearchFilter
{
    /// <summary>Case-insensitive text matched against the doctor's name.</summary>
    public string? SearchTerm { get; set; }

    public int? SpecialtyId { get; set; }

    public decimal? MinFee { get; set; }

    public decimal? MaxFee { get; set; }

    public DoctorSortOrder Sort { get; set; } = DoctorSortOrder.NameAscending;
}
