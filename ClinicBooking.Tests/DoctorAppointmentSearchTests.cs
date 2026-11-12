using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class DoctorAppointmentSearchTests
{
    // A fixed future "now" so Today/Upcoming/Past are deterministic and isolated
    // from appointments created by other tests sharing the database.
    private static readonly DateTime Now = new(2030, 6, 15, 9, 0, 0);

    private readonly TestDatabase _db;

    public DoctorAppointmentSearchTests(TestDatabase db) => _db = db;

    private static AppointmentService NewService(ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateMapper());

    private static async Task<Doctor> AddDoctorAsync(ApplicationDbContext ctx)
    {
        var doctor = TestData.NewDoctor();
        ctx.Doctors.Add(doctor);
        await ctx.SaveChangesAsync();
        return doctor;
    }

    private static async Task<Patient> AddPatientAsync(ApplicationDbContext ctx, string? name = null)
    {
        var patient = TestData.NewPatient();
        if (name is not null) patient.FullName = name;
        ctx.Patients.Add(patient);
        await ctx.SaveChangesAsync();
        return patient;
    }

    private static Appointment NewAppointment(
        Doctor doctor,
        Patient patient,
        DateTime start,
        AppointmentStatus status,
        decimal? amount = null,
        PaymentStatus? paymentStatus = null)
    {
        var appointment = new Appointment
        {
            Doctor = doctor,
            Patient = patient,
            AppointmentDate = DateOnly.FromDateTime(start),
            StartTime = start.TimeOfDay,
            EndTime = start.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
            Status = status
        };

        if (amount is decimal value && paymentStatus is PaymentStatus ps)
        {
            appointment.Payment = new Payment
            {
                Amount = value,
                Currency = "EGP",
                Method = PaymentMethod.Cash,
                Status = ps
            };
        }

        return appointment;
    }

    private static async Task AddAppointmentsAsync(ApplicationDbContext ctx, params Appointment[] appointments)
    {
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Search_ByStatusTab_ReturnsOnlyMatchingAppointments()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, Now.AddDays(1), AppointmentStatus.Pending),
            NewAppointment(doctor, patient, Now.AddDays(2), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, Now.AddDays(3), AppointmentStatus.Completed),
            NewAppointment(doctor, patient, Now.AddDays(4), AppointmentStatus.Cancelled));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id,
            new DoctorAppointmentFilter { Tab = DoctorAppointmentTab.Pending, PageSize = 50 },
            Now);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, a => Assert.Equal(AppointmentStatus.Pending, a.Status));
    }

    [Fact]
    public async Task Search_ByTodayTab_ReturnsOnlyTodaysAppointments()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, Now.Date.AddHours(10), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, Now.Date.AddDays(1).AddHours(10), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, Now.Date.AddDays(-1).AddHours(10), AppointmentStatus.Confirmed));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id,
            new DoctorAppointmentFilter { Tab = DoctorAppointmentTab.Today, PageSize = 50 },
            Now);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(DateOnly.FromDateTime(Now), result.Items[0].AppointmentDate);
    }

    [Fact]
    public async Task Search_ByUpcomingAndPastTabs_SplitOnStartTime()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, Now.Date.AddHours(8), AppointmentStatus.Confirmed),   // past (today, earlier)
            NewAppointment(doctor, patient, Now.Date.AddHours(10), AppointmentStatus.Confirmed),  // upcoming (today, later)
            NewAppointment(doctor, patient, Now.Date.AddDays(1).AddHours(10), AppointmentStatus.Pending), // upcoming
            NewAppointment(doctor, patient, Now.Date.AddDays(-1).AddHours(10), AppointmentStatus.Completed)); // past

        var service = NewService(ctx);

        var upcoming = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id, new DoctorAppointmentFilter { Tab = DoctorAppointmentTab.Upcoming, PageSize = 50 }, Now);
        var past = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id, new DoctorAppointmentFilter { Tab = DoctorAppointmentTab.Past, PageSize = 50 }, Now);

        Assert.Equal(2, upcoming.TotalCount);
        Assert.Equal(2, past.TotalCount);
    }

    [Fact]
    public async Task Search_ByPatientName_ReturnsMatchesOnly()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var token = Guid.NewGuid().ToString("N")[..8];
        var target = await AddPatientAsync(ctx, $"Target {token} Patient");
        var other = await AddPatientAsync(ctx, $"Unrelated {token} Person");

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, target, Now.AddDays(1), AppointmentStatus.Confirmed),
            NewAppointment(doctor, other, Now.AddDays(2), AppointmentStatus.Confirmed));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id,
            new DoctorAppointmentFilter { SearchTerm = "Target", PageSize = 50 },
            Now);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(target.FullName, result.Items[0].PatientName);
    }

    [Fact]
    public async Task Search_ByDateRange_ReturnsAppointmentsWithinRange()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, new DateTime(2030, 6, 1, 10, 0, 0), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, new DateTime(2030, 6, 15, 10, 0, 0), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, new DateTime(2030, 7, 1, 10, 0, 0), AppointmentStatus.Confirmed));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id,
            new DoctorAppointmentFilter
            {
                FromDate = new DateOnly(2030, 6, 10),
                ToDate = new DateOnly(2030, 6, 20),
                PageSize = 50
            },
            Now);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(new DateOnly(2030, 6, 15), result.Items[0].AppointmentDate);
    }

    [Fact]
    public async Task Search_DefaultSort_IsNewestFirst()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, Now.Date.AddDays(3).AddHours(10), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, Now.Date.AddDays(1).AddHours(10), AppointmentStatus.Confirmed),
            NewAppointment(doctor, patient, Now.Date.AddDays(2).AddHours(10), AppointmentStatus.Confirmed));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id, new DoctorAppointmentFilter { PageSize = 50 }, Now);

        Assert.Equal(
            new[] { Now.Date.AddDays(3), Now.Date.AddDays(2), Now.Date.AddDays(1) },
            result.Items.Select(a => a.AppointmentDate.ToDateTime(TimeOnly.MinValue)).ToArray());
    }

    [Fact]
    public async Task Search_SortByPatientName_OrdersAlphabetically()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var token = Guid.NewGuid().ToString("N")[..8];
        var charlie = await AddPatientAsync(ctx, $"Charlie {token}");
        var alice = await AddPatientAsync(ctx, $"Alice {token}");
        var bob = await AddPatientAsync(ctx, $"Bob {token}");

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, charlie, Now.Date.AddDays(1).AddHours(9), AppointmentStatus.Confirmed),
            NewAppointment(doctor, alice, Now.Date.AddDays(1).AddHours(10), AppointmentStatus.Confirmed),
            NewAppointment(doctor, bob, Now.Date.AddDays(1).AddHours(11), AppointmentStatus.Confirmed));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id,
            new DoctorAppointmentFilter { Sort = DoctorAppointmentSortOrder.PatientName, PageSize = 50 },
            Now);

        Assert.Equal(new[] { alice.FullName, bob.FullName, charlie.FullName },
            result.Items.Select(a => a.PatientName).ToArray());
    }

    [Fact]
    public async Task Search_Paginates_AndReportsTotals()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        for (var day = 1; day <= 5; day++)
        {
            await AddAppointmentsAsync(ctx,
                NewAppointment(doctor, patient, Now.Date.AddDays(day).AddHours(10), AppointmentStatus.Confirmed));
        }

        var service = NewService(ctx);

        var firstPage = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id, new DoctorAppointmentFilter { Page = 1, PageSize = 2 }, Now);

        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNext);
        Assert.False(firstPage.HasPrevious);

        var lastPage = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id, new DoctorAppointmentFilter { Page = 3, PageSize = 2 }, Now);

        Assert.Single(lastPage.Items);
        Assert.True(lastPage.HasPrevious);
        Assert.False(lastPage.HasNext);
    }

    [Fact]
    public async Task Search_IsScopedToTheRequestedDoctor()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var otherDoctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, Now.Date.AddDays(1).AddHours(10), AppointmentStatus.Confirmed),
            NewAppointment(otherDoctor, patient, Now.Date.AddDays(1).AddHours(11), AppointmentStatus.Confirmed));

        var service = NewService(ctx);
        var result = await service.SearchAppointmentsForDoctorAsync(
            doctor.Id, new DoctorAppointmentFilter { PageSize = 50 }, Now);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(doctor.Id, result.Items[0].DoctorId);
    }

    [Fact]
    public async Task Stats_CountsByStatusAndSumsPaymentTotals()
    {
        using var ctx = _db.CreateContext();
        var doctor = await AddDoctorAsync(ctx);
        var patient = await AddPatientAsync(ctx);

        await AddAppointmentsAsync(ctx,
            NewAppointment(doctor, patient, Now.Date.AddHours(10), AppointmentStatus.Confirmed, 200m, PaymentStatus.Paid),
            NewAppointment(doctor, patient, Now.Date.AddDays(1).AddHours(10), AppointmentStatus.Pending, 150m, PaymentStatus.Pending),
            NewAppointment(doctor, patient, Now.Date.AddDays(2).AddHours(10), AppointmentStatus.Completed, 300m, PaymentStatus.Paid),
            NewAppointment(doctor, patient, Now.Date.AddDays(-1).AddHours(10), AppointmentStatus.Cancelled));

        var service = NewService(ctx);
        var stats = await service.GetAppointmentStatsForDoctorAsync(doctor.Id, Now);

        Assert.Equal(4, stats.Total);
        Assert.Equal(1, stats.Today);
        Assert.Equal(3, stats.Upcoming);   // today 10:00 + the next two days
        Assert.Equal(1, stats.Past);       // yesterday
        Assert.Equal(1, stats.Pending);
        Assert.Equal(1, stats.Confirmed);
        Assert.Equal(1, stats.Completed);
        Assert.Equal(1, stats.Cancelled);
        Assert.Equal(500m, stats.CollectedAmount);
        Assert.Equal(150m, stats.OutstandingAmount);
    }
}
