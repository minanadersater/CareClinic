using AutoMapper;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class DoctorService : IDoctorService, ClinicBooking.Application.Services.IDoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public DoctorService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        return await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Doctor>> GetActiveAsync()
    {
        return await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .Where(d => d.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor?> GetByUserIdAsync(string userId)
    {
        return await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .FirstOrDefaultAsync(d => d.UserId == userId);
    }

    public async Task<DoctorDto?> GetDoctorByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .FirstOrDefaultAsync(d => d.UserId == userId, ct);

        return doctor is null ? null : _mapper.Map<DoctorDto>(doctor);
    }

    public async Task<DoctorDto?> GetDoctorByIdAsync(int id, CancellationToken ct = default)
    {
        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return doctor is null ? null : _mapper.Map<DoctorDto>(doctor);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetAllDoctorsAsync(CancellationToken ct = default)
    {
        var doctors = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<DoctorDto>>(doctors);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetDoctorsBySpecialtyAsync(int specialtyId, CancellationToken ct = default)
    {
        var doctors = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .Where(d => d.IsActive && d.DoctorSpecialties.Any(ds => ds.SpecialtyId == specialtyId))
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<DoctorDto>>(doctors);
    }

    public async Task<Doctor> CreateDoctorAsync(CreateDoctorDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            EmailConfirmed = true,
            IsActive = true
        };

        var identityResult = await _userManager.CreateAsync(user, dto.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user account: {errors}");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Doctor");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new InvalidOperationException("Failed to assign Doctor role to user account.");
        }

        var doctor = new Doctor
        {
            UserId = user.Id,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Bio = dto.Bio,
            ConsultationFee = dto.ConsultationFee,
            ClinicName = dto.ClinicName,
            ClinicAddress = dto.ClinicAddress,
            AppointmentDurationMinutes = dto.AppointmentDurationMinutes > 0 ? dto.AppointmentDurationMinutes : 30,
            IsActive = true
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        if (dto.SpecialtyIds != null && dto.SpecialtyIds.Any())
        {
            foreach (var specId in dto.SpecialtyIds)
            {
                _context.DoctorSpecialties.Add(new DoctorSpecialty
                {
                    DoctorId = doctor.Id,
                    SpecialtyId = specId
                });
            }
        }

        // Add default working schedule (Sun-Thu 09:00 - 17:00)
        foreach (WeekDay day in Enum.GetValues<WeekDay>())
        {
            _context.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = day,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17),
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
        return doctor;
    }

    public async Task UpdateDoctorAsync(UpdateDoctorDto dto, CancellationToken ct = default)
    {
        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
            .FirstOrDefaultAsync(d => d.Id == dto.Id, ct)
            ?? throw new KeyNotFoundException($"Doctor {dto.Id} not found.");

        doctor.FullName = dto.FullName;
        doctor.PhoneNumber = dto.PhoneNumber;
        doctor.Bio = dto.Bio;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.ClinicName = dto.ClinicName;
        doctor.ClinicAddress = dto.ClinicAddress;
        doctor.AppointmentDurationMinutes = dto.AppointmentDurationMinutes > 0 ? dto.AppointmentDurationMinutes : 30;
        doctor.IsActive = dto.IsActive;

        // Update specialties
        var existingSpecs = doctor.DoctorSpecialties.ToList();
        _context.DoctorSpecialties.RemoveRange(existingSpecs);

        if (dto.SpecialtyIds != null && dto.SpecialtyIds.Any())
        {
            foreach (var specId in dto.SpecialtyIds)
            {
                _context.DoctorSpecialties.Add(new DoctorSpecialty
                {
                    DoctorId = doctor.Id,
                    SpecialtyId = specId
                });
            }
        }

        // Also update Identity user FullName / PhoneNumber if present
        var user = await _userManager.FindByIdAsync(doctor.UserId);
        if (user != null)
        {
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;
            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateProfileAsync(Doctor doctor)
    {
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync();
    }

    public async Task AssignSpecialtiesAsync(int doctorId, List<int> specialtyIds)
    {
        var existing = await _context.DoctorSpecialties.Where(ds => ds.DoctorId == doctorId).ToListAsync();
        _context.DoctorSpecialties.RemoveRange(existing);

        foreach (var specId in specialtyIds)
        {
            _context.DoctorSpecialties.Add(new DoctorSpecialty
            {
                DoctorId = doctorId,
                SpecialtyId = specId
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveStatusAsync(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor != null)
        {
            doctor.IsActive = !doctor.IsActive;
            var user = await _userManager.FindByIdAsync(doctor.UserId);
            if (user != null)
            {
                user.IsActive = doctor.IsActive;
                await _userManager.UpdateAsync(user);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task SetActiveStatusAsync(int doctorId, bool isActive, CancellationToken ct = default)
    {
        var doctor = await _context.Doctors.FindAsync(new object[] { doctorId }, ct);
        if (doctor != null)
        {
            doctor.IsActive = isActive;
            var user = await _userManager.FindByIdAsync(doctor.UserId);
            if (user != null)
            {
                user.IsActive = isActive;
                await _userManager.UpdateAsync(user);
            }
            await _context.SaveChangesAsync(ct);
        }
    }
}
