using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface IScheduleService
{
    Task<IEnumerable<DoctorSchedule>> GetDoctorSchedulesAsync(int doctorId);
    Task UpdateDoctorScheduleAsync(int doctorId, List<DoctorSchedule> schedules);
    Task<IReadOnlyList<DoctorScheduleDto>> GetScheduleForDoctorAsync(int doctorId, CancellationToken ct = default);
    Task UpsertScheduleAsync(UpsertScheduleDto dto, CancellationToken ct = default);
    Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default);
}
