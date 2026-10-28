using ClinicBooking.Application.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardDataAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new AdminDashboardDto
        {
            TotalDoctors = await _context.Doctors.CountAsync(),
            ActiveDoctors = await _context.Doctors.CountAsync(d => d.IsActive),
            TotalPatients = await _context.Patients.CountAsync(),
            TodaysAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate == today),
            CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed),
            CancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled)
        };
    }

    public async Task<DoctorDashboardDto> GetDoctorDashboardDataAsync(int doctorId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new DoctorDashboardDto
        {
            TodaysAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate == today),
            CompletedAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctorId && a.Status == AppointmentStatus.Completed),
            TotalPatients = await _context.Appointments.Where(a => a.DoctorId == doctorId).Select(a => a.PatientId).Distinct().CountAsync()
        };
    }

    public async Task<PatientDashboardDto> GetPatientDashboardDataAsync(int patientId)
    {
        return new PatientDashboardDto
        {
            TotalAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patientId),
            MedicalRecordsCount = await _context.MedicalRecords.CountAsync(m => m.PatientId == patientId)
        };
    }
}
