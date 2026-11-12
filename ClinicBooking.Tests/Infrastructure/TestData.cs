using AutoMapper;
using ClinicBooking.Application.Mappings;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClinicBooking.Tests.Infrastructure;

/// <summary>Factory helpers that keep test arrange blocks small and readable.</summary>
public static class TestData
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    public static IMapper CreateMapper() => Mapper;

    /// <summary>
    /// Builds a real <see cref="UserManager{TUser}"/> over the supplied context so
    /// services that depend on Identity can be constructed in tests without a host.
    /// </summary>
    public static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, string>(context);

        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            logger: NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    public static Doctor NewDoctor(decimal fee = 250m, int durationMinutes = 30, bool isActive = true) => new()
    {
        UserId = Guid.NewGuid().ToString(),
        FullName = "Dr. Test " + Guid.NewGuid().ToString("N")[..6],
        PhoneNumber = "01000000001",
        Email = $"dr.{Guid.NewGuid():N}@test.local",
        ConsultationFee = fee,
        AppointmentDurationMinutes = durationMinutes,
        IsActive = isActive
    };

    public static Patient NewPatient() => new()
    {
        UserId = Guid.NewGuid().ToString(),
        FullName = "Test Patient " + Guid.NewGuid().ToString("N")[..6],
        PhoneNumber = "01000000002",
        Email = $"patient.{Guid.NewGuid():N}@test.local"
    };

    /// <summary>
    /// Persists a doctor working 09:00-17:00 every day plus a patient, so a test
    /// can book any weekday at a known slot (10:00 is used throughout).
    /// </summary>
    public static async Task<(Doctor Doctor, Patient Patient)> SeedDoctorAndPatientAsync(
        ApplicationDbContext context,
        decimal fee = 250m,
        int durationMinutes = 30)
    {
        var doctor = NewDoctor(fee, durationMinutes);
        var patient = NewPatient();

        context.Doctors.Add(doctor);
        context.Patients.Add(patient);

        for (var day = 0; day < 7; day++)
        {
            context.DoctorSchedules.Add(new DoctorSchedule
            {
                Doctor = doctor,
                DayOfWeek = (WeekDay)day,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
        return (doctor, patient);
    }

    /// <summary>Tomorrow, so bookings are always in the future.</summary>
    public static DateTime Tomorrow => DateTime.Today.AddDays(1);

    public static readonly TimeSpan TenAm = new(10, 0, 0);
}
