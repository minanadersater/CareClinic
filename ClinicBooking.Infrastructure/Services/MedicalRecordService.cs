using AutoMapper;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class MedicalRecordService : IMedicalRecordService, ClinicBooking.Application.Services.IMedicalRecordService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public MedicalRecordService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId)
    {
        return await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.Items)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.RecordDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<MedicalRecord>> GetByDoctorIdAsync(int doctorId)
    {
        return await _context.MedicalRecords
            .Include(m => m.Patient)
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.Items)
            .Where(m => m.DoctorId == doctorId)
            .OrderByDescending(m => m.RecordDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<MedicalRecord?> GetByAppointmentIdAsync(int appointmentId)
    {
        return await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Patient)
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);
    }

    public async Task<MedicalRecord?> GetByIdAsync(int id)
    {
        return await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Patient)
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> GetRecordsForPatientAsync(int patientId, CancellationToken ct = default)
    {
        var records = await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Patient)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.RecordDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<MedicalRecordDto>>(records);
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> GetRecordsForDoctorAsync(int doctorId, CancellationToken ct = default)
    {
        var records = await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Patient)
            .Where(m => m.DoctorId == doctorId)
            .OrderByDescending(m => m.RecordDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<MedicalRecordDto>>(records);
    }

    public async Task<MedicalRecordDto?> GetRecordDtoByIdAsync(int id, CancellationToken ct = default)
    {
        var record = await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Patient)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return record is null ? null : _mapper.Map<MedicalRecordDto>(record);
    }

    public async Task<MedicalRecord> CreateRecordWithPrescriptionAsync(MedicalRecord record, Prescription? prescription)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            record.RecordDate = DateTime.UtcNow;
            record.CreatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;

            _context.MedicalRecords.Add(record);
            await _context.SaveChangesAsync();

            if (prescription != null && prescription.Items != null && prescription.Items.Any())
            {
                prescription.MedicalRecordId = record.Id;
                prescription.PatientId = record.PatientId;
                prescription.DoctorId = record.DoctorId;
                prescription.IssuedDate = DateTime.UtcNow;
                prescription.CreatedAt = DateTime.UtcNow;
                prescription.UpdatedAt = DateTime.UtcNow;

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return record;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<MedicalRecordDto> CreateAsync(CreateMedicalRecordDto dto, CancellationToken ct = default)
    {
        var record = _mapper.Map<MedicalRecord>(dto);
        record.RecordDate = DateTime.UtcNow;
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync(ct);

        return _mapper.Map<MedicalRecordDto>(record);
    }
}
