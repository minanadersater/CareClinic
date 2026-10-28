using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Interfaces;

public interface IDoctorService
{
    Task<IEnumerable<Doctor>> GetAllAsync();
    Task<IEnumerable<Doctor>> GetActiveAsync();
    Task<Doctor?> GetByIdAsync(int id);
    Task<Doctor?> GetByUserIdAsync(string userId);
    Task<DoctorDto?> GetDoctorByUserIdAsync(string userId, CancellationToken ct = default);
    Task<DoctorDto?> GetDoctorByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DoctorDto>> GetAllDoctorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DoctorDto>> GetDoctorsBySpecialtyAsync(int specialtyId, CancellationToken ct = default);
    Task<Doctor> CreateDoctorAsync(CreateDoctorDto dto);
    Task UpdateDoctorAsync(UpdateDoctorDto dto, CancellationToken ct = default);
    Task UpdateProfileAsync(Doctor doctor);
    Task AssignSpecialtiesAsync(int doctorId, List<int> specialtyIds);
    Task ToggleActiveStatusAsync(int id);
    Task SetActiveStatusAsync(int doctorId, bool isActive, CancellationToken ct = default);
}
