using AutoMapper;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class PatientService : IPatientService, ClinicBooking.Application.Services.IPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public PatientService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PatientDto>> GetAllPatientsAsync(CancellationToken ct = default)
    {
        var patients = await _context.Patients
            .Include(p => p.Appointments)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<PatientDto>>(patients);
    }

    public async Task<PatientDto?> GetPatientByIdAsync(int id, CancellationToken ct = default)
    {
        var patient = await _context.Patients
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return patient is null ? null : _mapper.Map<PatientDto>(patient);
    }

    public async Task<PatientDto?> GetPatientByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var patient = await _context.Patients
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        return patient is null ? null : _mapper.Map<PatientDto>(patient);
    }

    public async Task<Patient?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Patients
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Patient?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Patients
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public async Task UpdatePatientAsync(UpdatePatientDto dto, CancellationToken ct = default)
    {
        var patient = await _context.Patients.FindAsync(new object[] { dto.Id }, ct)
            ?? throw new KeyNotFoundException($"Patient {dto.Id} not found.");

        _mapper.Map(dto, patient);
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync(ct);
    }
}
