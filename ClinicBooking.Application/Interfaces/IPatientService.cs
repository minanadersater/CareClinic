using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface IPatientService
{
    Task<IReadOnlyList<PatientDto>> GetAllPatientsAsync(CancellationToken ct = default);
    Task<PatientDto?> GetPatientByIdAsync(int id, CancellationToken ct = default);
    Task<PatientDto?> GetPatientByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Patient?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Patient?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task UpdatePatientAsync(UpdatePatientDto dto, CancellationToken ct = default);
}
