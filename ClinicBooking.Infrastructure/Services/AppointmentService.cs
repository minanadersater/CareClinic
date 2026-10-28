using System.Data;
using AutoMapper;
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
        var today = DateOnly.FromDateTime(DateTime.Today);
        var nowTime = DateTime.Now.TimeOfDay;

        while (currentTime.Add(TimeSpan.FromMinutes(slotDuration)) <= schedule.EndTime)
        {
            var slotEnd = currentTime.Add(TimeSpan.FromMinutes(slotDuration));
            bool isPast = (targetDate == today && currentTime <= nowTime);

            bool isBooked = bookedAppointments.Any(b =>
                (currentTime >= b.StartTime && currentTime < b.EndTime) ||
                (slotEnd > b.StartTime && slotEnd <= b.EndTime) ||
                (currentTime <= b.StartTime && slotEnd >= b.EndTime));

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
        var targetDate = DateOnly.FromDateTime(date);
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId && d.IsActive)
                ?? throw new InvalidOperationException("Doctor is not active or available.");

            int slotDuration = doctor.AppointmentDurationMinutes > 0 ? doctor.AppointmentDurationMinutes : 30;
            var endTime = startTime.Add(TimeSpan.FromMinutes(slotDuration));

            var targetWeekDay = (WeekDay)(int)date.DayOfWeek;
            var schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == targetWeekDay && s.IsActive)
                ?? throw new InvalidOperationException("Doctor is not scheduled to work on this day.");

            if (startTime < schedule.StartTime || endTime > schedule.EndTime)
                throw new InvalidOperationException("Selected time is outside doctor's working hours.");

            // Check if slot already booked
            bool isDoubleBooked = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId && a.AppointmentDate == targetDate &&
                               a.Status != AppointmentStatus.Cancelled &&
                               ((startTime >= a.StartTime && startTime < a.EndTime) ||
                                (endTime > a.StartTime && endTime <= a.EndTime)));

            if (isDoubleBooked)
                throw new InvalidOperationException("This time slot has already been booked by another patient. Please select another slot.");

            // Check patient overlapping appointment
            bool patientOverlap = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId && a.AppointmentDate == targetDate &&
                               a.Status != AppointmentStatus.Cancelled &&
                               ((startTime >= a.StartTime && startTime < a.EndTime) ||
                                (endTime > a.StartTime && endTime <= a.EndTime)));

            if (patientOverlap)
                throw new InvalidOperationException("You already have an appointment scheduled during this time.");

            var appointment = new Appointment
            {
                DoctorId = doctorId,
                PatientId = patientId,
                AppointmentDate = targetDate,
                StartTime = startTime,
                EndTime = endTime,
                Status = paymentMethod == PaymentMethod.Cash ? AppointmentStatus.Pending : AppointmentStatus.Confirmed,
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
                Status = paymentMethod == PaymentMethod.Cash ? PaymentStatus.Pending : PaymentStatus.Paid,
                TransactionId = transactionId ?? (paymentMethod == PaymentMethod.Cash ? null : $"TXN-EGP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"),
                PaidAt = paymentMethod == PaymentMethod.Cash ? null : DateTime.UtcNow,
                PaymentNotes = paymentNotes ?? (paymentMethod == PaymentMethod.Cash ? "الدفع نقداً في العيادة" : "تم الدفع إلكترونياً"),
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
}
