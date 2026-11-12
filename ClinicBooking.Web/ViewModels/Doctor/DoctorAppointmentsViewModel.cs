using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.DTOs;

namespace ClinicBooking.Web.ViewModels.Doctor;

/// <summary>
/// Backs the doctor's appointments screen: the active filter, the current page of
/// results, and the headline stats shown above the table.
/// </summary>
public class DoctorAppointmentsViewModel
{
    public DoctorAppointmentFilter Filter { get; set; } = new();

    public PagedResult<AppointmentDto> Appointments { get; set; } =
        PagedResult<AppointmentDto>.Empty(1, 10);

    public DoctorAppointmentStats Stats { get; set; } = new();
}
