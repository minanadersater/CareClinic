namespace ClinicBooking.Application.Common.DTOs;

/// <summary>
/// The quick-filter tab selected on the doctor's appointments screen. Scope tabs
/// (Today/Upcoming/Past) are time-based; status tabs match a single
/// <see cref="ClinicBooking.Domain.Enums.AppointmentStatus"/>.
/// </summary>
public enum DoctorAppointmentTab
{
    All = 0,
    Today = 1,
    Upcoming = 2,
    Past = 3,
    Pending = 4,
    Confirmed = 5,
    Completed = 6,
    Cancelled = 7
}

/// <summary>Ordering options for the doctor's appointment list.</summary>
public enum DoctorAppointmentSortOrder
{
    DateDescending = 0,
    DateAscending = 1,
    PatientName = 2
}

/// <summary>
/// Criteria used to narrow a doctor's appointment list. Every field is optional;
/// a default filter returns the doctor's most recent appointments first.
/// </summary>
public class DoctorAppointmentFilter
{
    public DoctorAppointmentTab Tab { get; set; } = DoctorAppointmentTab.All;

    /// <summary>Case-insensitive text matched against the patient's name.</summary>
    public string? SearchTerm { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public DoctorAppointmentSortOrder Sort { get; set; } = DoctorAppointmentSortOrder.DateDescending;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    /// <summary>True when any filter beyond the default is active.</summary>
    public bool HasFilters =>
        Tab != DoctorAppointmentTab.All ||
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        FromDate is not null ||
        ToDate is not null;
}

/// <summary>
/// Headline figures shown above the doctor's appointment list. Counts cover all
/// of the doctor's appointments (not just the current page or filter).
/// </summary>
public class DoctorAppointmentStats
{
    public int Total { get; set; }
    public int Today { get; set; }
    public int Upcoming { get; set; }
    public int Past { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }

    /// <summary>Total cash/e-payment amount already collected.</summary>
    public decimal CollectedAmount { get; set; }

    /// <summary>Total amount still outstanding (pending payments).</summary>
    public decimal OutstandingAmount { get; set; }

    public string Currency { get; set; } = "EGP";
}
