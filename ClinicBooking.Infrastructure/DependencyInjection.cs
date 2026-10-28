using ClinicBooking.Application.Common.Interfaces;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Infrastructure.Repositories;
using ClinicBooking.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<ClinicBooking.Application.Services.IDoctorService>(sp =>
            (ClinicBooking.Application.Services.IDoctorService)sp.GetRequiredService<IDoctorService>());

        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ClinicBooking.Application.Services.IPatientService>(sp =>
            (ClinicBooking.Application.Services.IPatientService)sp.GetRequiredService<IPatientService>());

        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ClinicBooking.Application.Services.IAppointmentService>(sp =>
            (ClinicBooking.Application.Services.IAppointmentService)sp.GetRequiredService<IAppointmentService>());

        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<ClinicBooking.Application.Services.IScheduleService>(sp =>
            (ClinicBooking.Application.Services.IScheduleService)sp.GetRequiredService<IScheduleService>());

        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<ClinicBooking.Application.Services.IMedicalRecordService>(sp =>
            (ClinicBooking.Application.Services.IMedicalRecordService)sp.GetRequiredService<IMedicalRecordService>());

        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ClinicBooking.Application.Services.IPaymentService>(sp =>
            (ClinicBooking.Application.Services.IPaymentService)sp.GetRequiredService<IPaymentService>());

        return services;
    }
}
