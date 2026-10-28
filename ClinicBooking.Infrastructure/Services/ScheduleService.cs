using AutoMapper;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class ScheduleService : IScheduleService, ClinicBooking.Application.Services.IScheduleService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ScheduleService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DoctorSchedule>> GetDoctorSchedulesAsync(int doctorId)
    {
        return await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateDoctorScheduleAsync(int doctorId, List<DoctorSchedule> schedules)
    {
        var existing = await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId)
            .ToListAsync();

        _context.DoctorSchedules.RemoveRange(existing);

        foreach (var schedule in schedules)
        {
            schedule.DoctorId = doctorId;
            schedule.CreatedAt = DateTime.UtcNow;
            schedule.UpdatedAt = DateTime.UtcNow;
            _context.DoctorSchedules.Add(schedule);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<DoctorScheduleDto>> GetScheduleForDoctorAsync(int doctorId, CancellationToken ct = default)
    {
        var schedules = await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<DoctorScheduleDto>>(schedules);
    }

    public async Task UpsertScheduleAsync(UpsertScheduleDto dto, CancellationToken ct = default)
    {
        if (dto.Id.HasValue && dto.Id.Value > 0)
        {
            var existing = await _context.DoctorSchedules.FindAsync(new object[] { dto.Id.Value }, ct)
                ?? throw new KeyNotFoundException($"Schedule {dto.Id.Value} not found.");

            _mapper.Map(dto, existing);
            existing.UpdatedAt = DateTime.UtcNow;
            _context.DoctorSchedules.Update(existing);
        }
        else
        {
            var schedule = _mapper.Map<DoctorSchedule>(dto);
            schedule.CreatedAt = DateTime.UtcNow;
            schedule.UpdatedAt = DateTime.UtcNow;
            _context.DoctorSchedules.Add(schedule);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default)
    {
        var schedule = await _context.DoctorSchedules.FindAsync(new object[] { scheduleId }, ct)
            ?? throw new KeyNotFoundException($"Schedule {scheduleId} not found.");

        _context.DoctorSchedules.Remove(schedule);
        await _context.SaveChangesAsync(ct);
    }
}
