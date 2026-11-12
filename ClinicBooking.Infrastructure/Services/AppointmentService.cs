using System.Data;
using AutoMapper;
using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class AppointmentService : IAppointmentService, ClinicBooking.Application.Services.IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AppointmentService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAllAppointmentsAsync(CancellationToken ct = default)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Payment)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<AppointmentDto>>(appointments);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAppointmentsForDoctorAsync(int doctorId, CancellationToken ct = default)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Payment)
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<AppointmentDto>>(appointments);
    }

    public async Task<PagedResult<AppointmentDto>> SearchAppointmentsForDoctorAsync(
        int doctorId,
        DoctorAppointmentFilter filter,
        DateTime now,
        CancellationToken ct = default)
    {
        filter ??= new DoctorAppointmentFilter();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is > 0 and <= 100 ? filter.PageSize : 10;

        var today = DateOnly.FromDateTime(now);
        var nowTime = now.TimeOfDay;

        var query = _context.Appointments.Where(a => a.DoctorId == doctorId);

        query = filter.Tab switch
        {
            DoctorAppointmentTab.Today => query.Where(a => a.AppointmentDate == today),
            DoctorAppointmentTab.Upcoming => query.Where(a =>
                a.AppointmentDate > today ||
                (a.AppointmentDate == today && a.StartTime >= nowTime)),
            DoctorAppointmentTab.Past => query.Where(a =>
                a.AppointmentDate < today ||
                (a.AppointmentDate == today && a.StartTime < nowTime)),
            DoctorAppointmentTab.Pending => query.Where(a => a.Status == AppointmentStatus.Pending),
            DoctorAppointmentTab.Confirmed => query.Where(a => a.Status == AppointmentStatus.Confirmed),
            DoctorAppointmentTab.Completed => query.Where(a => a.Status == AppointmentStatus.Completed),
            DoctorAppointmentTab.Cancelled => query.Where(a => a.Status == AppointmentStatus.Cancelled),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(a => a.Patient.FullName.Contains(term));
        }

        if (filter.FromDate is { } from)
        {
            query = query.Where(a => a.AppointmentDate >= from);
        }

        if (filter.ToDate is { } to)
        {
            query = query.Where(a => a.AppointmentDate <= to);
        }

        var totalCount = await query.CountAsync(ct);

        query = filter.Sort switch
        {
            DoctorAppointmentSortOrder.DateAscending =>
                query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime),
            DoctorAppointmentSortOrder.PatientName =>
                query.OrderBy(a => a.Patient.FullName).ThenBy(a => a.AppointmentDate).ThenBy(a => a.StartTime),
            _ =>
                query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
        };

        var appointments = await query
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Payment)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PagedResult<AppointmentDto>
        {
            Items = _mapper.Map<IReadOnlyList<AppointmentDto>>(appointments),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DoctorAppointmentStats> GetAppointmentStatsForDoctorAsync(
        int doctorId,
        DateTime now,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var nowTime = now.TimeOfDay;

        // Project only the columns needed for the aggregates so the whole row set
        // is not materialised.
        var rows = await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Select(a => new
            {
                a.AppointmentDate,
                a.StartTime,
                a.Status,
                PaymentStatus = a.Payment != null ? a.Payment.Status : (PaymentStatus?)null,
                Amount = a.Payment != null ? a.Payment.Amount : (decimal?)null
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var stats = new DoctorAppointmentStats { Total = rows.Count };

        foreach (var row in rows)
        {
            if (row.AppointmentDate == today) stats.Today++;

            var isUpcoming = row.AppointmentDate > today ||
                             (row.AppointmentDate == today && row.StartTime >= nowTime);
            if (isUpcoming) stats.Upcoming++;
            else stats.Past++;

            switch (row.Status)
            {
                case AppointmentStatus.Pending: stats.Pending++; break;
                case AppointmentStatus.Confirmed: stats.Confirmed++; break;
                case AppointmentStatus.Completed: stats.Completed++; break;
                case AppointmentStatus.Cancelled: stats.Cancelled++; break;
            }

            if (row.Amount is decimal amount)
            {
                if (row.PaymentStatus == PaymentStatus.Paid)
                {
                    stats.CollectedAmount += amount;
                }
                else if (row.PaymentStatus == PaymentStatus.Pending)
                {
                    stats.OutstandingAmount += amount;
                }
            }
        }

        return stats;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAppointmentsForPatientAsync(int patientId, CancellationToken ct = default)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Payment)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<AppointmentDto>>(appointments);
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id, CancellationToken ct = default)
    {
        var appt = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return appt is null ? null : _mapper.Map<AppointmentDto>(appt);
    }

    public async Task<IEnumerable<TimeSpan>> GetAvailableSlotsAsync(int doctorId, DateTime date)
    {
        var targetDate = DateOnly.FromDateTime(date);
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        if (targetDate < today)
            return Enumerable.Empty<TimeSpan>();

        var targetWeekDay = (WeekDay)(int)date.DayOfWeek;

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == targetWeekDay && s.IsActive);

        if (schedule == null)
            return Enumerable.Empty<TimeSpan>();

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
        int slotDuration = (doctor != null && doctor.AppointmentDurationMinutes > 0)
            ? doctor.AppointmentDurationMinutes
            : 30;

        var bookedAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == targetDate && a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var slots = new List<TimeSpan>();
        var currentTime = schedule.StartTime;
        var nowTime = now.TimeOfDay;

        while (currentTime.Add(TimeSpan.FromMinutes(slotDuration)) <= schedule.EndTime)
        {
            var slotEnd = currentTime.Add(TimeSpan.FromMinutes(slotDuration));
            bool isPast = (targetDate == today && currentTime <= nowTime);

            bool isBooked = bookedAppointments.Any(b => currentTime < b.EndTime && slotEnd > b.StartTime);

            if (!isPast && !isBooked)
            {
                slots.Add(currentTime);
            }

            currentTime = slotEnd;
        }

        return slots;
    }

    public async Task<Appointment> BookAppointmentAsync(
        int patientId,
        int doctorId,
        DateTime date,
        TimeSpan startTime,
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        string? paymentNotes = null,
        string? transactionId = null,
        string? patientNotes = null)
    {
        if (paymentMethod != PaymentMethod.Cash)
            throw new InvalidOperationException("Electronic payments are unavailable until a payment provider is configured.");

        var targetDate = DateOnly.FromDateTime(date);
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId && d.IsActive)
                ?? throw new InvalidOperationException("Doctor is not active or available.");

            var now = DateTime.Now;
            if (targetDate < DateOnly.FromDateTime(now) ||
                (targetDate == DateOnly.FromDateTime(now) && startTime <= now.TimeOfDay))
                throw new InvalidOperationException("Appointments cannot be booked for a past date or time.");

            if (startTime < TimeSpan.Zero || startTime >= TimeSpan.FromDays(1))
                throw new InvalidOperationException("Selected appointment time is invalid.");

            int slotDuration = doctor.AppointmentDurationMinutes > 0 ? doctor.AppointmentDurationMinutes : 30;
            var endTime = startTime.Add(TimeSpan.FromMinutes(slotDuration));

            var targetWeekDay = (WeekDay)(int)date.DayOfWeek;
            var schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == targetWeekDay && s.IsActive)
                ?? throw new InvalidOperationException("Doctor is not scheduled to work on this day.");

            var slotOffset = startTime - schedule.StartTime;
            if (slotOffset < TimeSpan.Zero || endTime > schedule.EndTime ||
                slotOffset.Ticks % TimeSpan.FromMinutes(slotDuration).Ticks != 0)
                throw new InvalidOperationException("Selected time is not an available appointment slot.");

            // Check if slot already booked
            bool isDoubleBooked = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId && a.AppointmentDate == targetDate &&
                               a.Status != AppointmentStatus.Cancelled &&
                               (startTime < a.EndTime && endTime > a.StartTime));

            if (isDoubleBooked)
                throw new InvalidOperationException("This time slot has already been booked by another patient. Please select another slot.");

            // Check patient overlapping appointment
            bool patientOverlap = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId && a.AppointmentDate == targetDate &&
                               a.Status != AppointmentStatus.Cancelled &&
                               (startTime < a.EndTime && endTime > a.StartTime));

            if (patientOverlap)
                throw new InvalidOperationException("You already have an appointment scheduled during this time.");

            var appointment = new Appointment
            {
                DoctorId = doctorId,
                PatientId = patientId,
                AppointmentDate = targetDate,
                StartTime = startTime,
                EndTime = endTime,
                Status = AppointmentStatus.Pending,
                PatientNotes = patientNotes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Create Payment record atomically
            var payment = new Payment
            {
                AppointmentId = appointment.Id,
                Amount = doctor.ConsultationFee,
                Currency = "EGP",
                Method = paymentMethod,
                Status = PaymentStatus.Pending,
                TransactionId = null,
                PaidAt = null,
                PaymentNotes = paymentNotes ?? "الدفع نقداً في العيادة",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            appointment.Payment = payment;
            return appointment;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AppointmentDto> BookAppointmentAsync(int patientId, CreateAppointmentDto dto, CancellationToken ct = default)
    {
        var appt = await BookAppointmentAsync(
            patientId,
            dto.DoctorId,
            dto.AppointmentDate.ToDateTime(TimeOnly.MinValue),
            dto.StartTime,
            dto.PaymentMethod,
            dto.PaymentNotes,
            dto.TransactionId,
            dto.PatientNotes);

        var loaded = await GetAppointmentByIdAsync(appt.Id, ct);
        return loaded!;
    }

    public async Task ConfirmAppointmentAsync(int appointmentId, CancellationToken ct = default)
    {
        var appt = await _context.Appointments.FindAsync(new object[] { appointmentId }, ct)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        appt.Status = AppointmentStatus.Confirmed;
        appt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task CompleteAppointmentAsync(int appointmentId, CancellationToken ct = default)
    {
        var appt = await _context.Appointments.FindAsync(new object[] { appointmentId }, ct)
            ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

        appt.Status = AppointmentStatus.Completed;
        appt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task CancelAppointmentAsync(CancelAppointmentDto dto, CancellationToken ct = default)
    {
        var appt = await _context.Appointments.FindAsync(new object[] { dto.AppointmentId }, ct)
            ?? throw new KeyNotFoundException($"Appointment {dto.AppointmentId} not found.");

        appt.Status = AppointmentStatus.Cancelled;
        appt.CancellationReason = dto.CancellationReason ?? "Cancelled by user";
        appt.CancelledAt = DateTime.UtcNow;
        appt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> CanPatientModifyAsync(int appointmentId, int cutoffHours, CancellationToken ct = default)
    {
        var appt = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appt is null)
            return false;

        if (appt.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            return false;

        return AppointmentPolicy.IsWithinChangeWindow(appt.AppointmentDate, appt.StartTime, DateTime.Now, cutoffHours);
    }

    public async Task CancelByPatientAsync(CancelAppointmentDto dto, int cutoffHours, CancellationToken ct = default)
    {
        var appt = await _context.Appointments.FindAsync(new object[] { dto.AppointmentId }, ct)
            ?? throw new KeyNotFoundException($"Appointment {dto.AppointmentId} not found.");

        if (appt.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            throw new InvalidOperationException("This appointment can no longer be changed.");

        if (!AppointmentPolicy.IsWithinChangeWindow(appt.AppointmentDate, appt.StartTime, DateTime.Now, cutoffHours))
            throw new InvalidOperationException(
                $"Appointments can only be cancelled at least {cutoffHours} hour(s) before the scheduled time. Please contact the clinic.");

        appt.Status = AppointmentStatus.Cancelled;
        appt.CancellationReason = dto.CancellationReason ?? "Cancelled by patient";
        appt.CancelledAt = DateTime.UtcNow;
        appt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<AppointmentDto> RescheduleAppointmentAsync(RescheduleAppointmentDto dto, int cutoffHours, CancellationToken ct = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var appt = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId, ct)
                ?? throw new KeyNotFoundException($"Appointment {dto.AppointmentId} not found.");

            if (appt.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                throw new InvalidOperationException("This appointment can no longer be changed.");

            if (!AppointmentPolicy.IsWithinChangeWindow(appt.AppointmentDate, appt.StartTime, DateTime.Now, cutoffHours))
                throw new InvalidOperationException(
                    $"Appointments can only be rescheduled at least {cutoffHours} hour(s) before the scheduled time. Please contact the clinic.");

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == appt.DoctorId && d.IsActive, ct)
                ?? throw new InvalidOperationException("Doctor is not active or available.");

            var now = DateTime.Now;
            var targetDate = dto.AppointmentDate;

            if (targetDate < DateOnly.FromDateTime(now) ||
                (targetDate == DateOnly.FromDateTime(now) && dto.StartTime <= now.TimeOfDay))
                throw new InvalidOperationException("Appointments cannot be moved to a past date or time.");

            if (dto.StartTime < TimeSpan.Zero || dto.StartTime >= TimeSpan.FromDays(1))
                throw new InvalidOperationException("Selected appointment time is invalid.");

            int slotDuration = doctor.AppointmentDurationMinutes > 0 ? doctor.AppointmentDurationMinutes : 30;
            var endTime = dto.StartTime.Add(TimeSpan.FromMinutes(slotDuration));

            var targetWeekDay = (WeekDay)(int)targetDate.DayOfWeek;
            var schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id && s.DayOfWeek == targetWeekDay && s.IsActive, ct)
                ?? throw new InvalidOperationException("Doctor is not scheduled to work on this day.");

            var slotOffset = dto.StartTime - schedule.StartTime;
            if (slotOffset < TimeSpan.Zero || endTime > schedule.EndTime ||
                slotOffset.Ticks % TimeSpan.FromMinutes(slotDuration).Ticks != 0)
                throw new InvalidOperationException("Selected time is not an available appointment slot.");

            // Exclude the appointment being moved so re-selecting its own slot is not
            // reported as a clash.
            bool isDoubleBooked = await _context.Appointments
                .AnyAsync(a => a.Id != appt.Id && a.DoctorId == doctor.Id && a.AppointmentDate == targetDate &&
                               a.Status != AppointmentStatus.Cancelled &&
                               dto.StartTime < a.EndTime && endTime > a.StartTime, ct);

            if (isDoubleBooked)
                throw new InvalidOperationException("This time slot has already been booked by another patient. Please select another slot.");

            bool patientOverlap = await _context.Appointments
                .AnyAsync(a => a.Id != appt.Id && a.PatientId == appt.PatientId && a.AppointmentDate == targetDate &&
                               a.Status != AppointmentStatus.Cancelled &&
                               dto.StartTime < a.EndTime && endTime > a.StartTime, ct);

            if (patientOverlap)
                throw new InvalidOperationException("You already have an appointment scheduled during this time.");

            appt.AppointmentDate = targetDate;
            appt.StartTime = dto.StartTime;
            appt.EndTime = endTime;
            // Moving an appointment invalidates any earlier confirmation.
            appt.Status = AppointmentStatus.Pending;
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return (await GetAppointmentByIdAsync(appt.Id, ct))!;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
