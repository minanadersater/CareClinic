using ClinicBooking.Application.DTOs;

namespace ClinicBooking.Application.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardDataAsync();
    Task<DoctorDashboardDto> GetDoctorDashboardDataAsync(int doctorId);
    Task<PatientDashboardDto> GetPatientDashboardDataAsync(int patientId);
}

