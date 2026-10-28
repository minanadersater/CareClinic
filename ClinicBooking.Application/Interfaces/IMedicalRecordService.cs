using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface IMedicalRecordService
{
    Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId);
    Task<IEnumerable<MedicalRecord>> GetByDoctorIdAsync(int doctorId);
    Task<MedicalRecord?> GetByAppointmentIdAsync(int appointmentId);
    Task<MedicalRecord?> GetByIdAsync(int id);
    Task<IReadOnlyList<MedicalRecordDto>> GetRecordsForPatientAsync(int patientId, CancellationToken ct = default);
    Task<IReadOnlyList<MedicalRecordDto>> GetRecordsForDoctorAsync(int doctorId, CancellationToken ct = default);
    Task<MedicalRecordDto?> GetRecordDtoByIdAsync(int id, CancellationToken ct = default);
    Task<MedicalRecord> CreateRecordWithPrescriptionAsync(MedicalRecord record, Prescription? prescription);
    Task<MedicalRecordDto> CreateAsync(CreateMedicalRecordDto dto, CancellationToken ct = default);
}
