using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class AppointmentLifecycleTests
{
    private readonly TestDatabase _db;

    public AppointmentLifecycleTests(TestDatabase db) => _db = db;

    private static AppointmentService NewService(ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateMapper());

    [Fact]
    public async Task ConfirmThenComplete_MovesAppointmentThroughLifecycle()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var appointment = await service.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);
        Assert.Equal(AppointmentStatus.Pending, appointment.Status);

        await service.ConfirmAppointmentAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Confirmed, (await service.GetAppointmentByIdAsync(appointment.Id))!.Status);

        await service.CompleteAppointmentAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Completed, (await service.GetAppointmentByIdAsync(appointment.Id))!.Status);
    }

    [Fact]
    public async Task CancelAppointment_SetsStatusReasonAndTimestamp()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var appointment = await service.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        await service.CancelAppointmentAsync(new CancelAppointmentDto
        {
            AppointmentId = appointment.Id,
            CancellationReason = "Patient felt better"
        });

        var stored = await ctx.Appointments.SingleAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Cancelled, stored.Status);
        Assert.Equal("Patient felt better", stored.CancellationReason);
        Assert.NotNull(stored.CancelledAt);
    }

    [Fact]
    public async Task CancelledAppointment_ReleasesTheSlotForRebooking()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var appointment = await service.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);
        await service.CancelAppointmentAsync(new CancelAppointmentDto { AppointmentId = appointment.Id });

        // The slot is available again...
        var slots = await service.GetAvailableSlotsAsync(doctor.Id, TestData.Tomorrow);
        Assert.Contains(TestData.TenAm, slots);

        // ...and a different patient can take it (also proves the filtered unique
        // index on the database allows a new active row at the same time).
        var otherPatient = TestData.NewPatient();
        ctx.Patients.Add(otherPatient);
        await ctx.SaveChangesAsync();

        var rebooked = await service.BookAppointmentAsync(
            otherPatient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        Assert.Equal(AppointmentStatus.Pending, rebooked.Status);
        Assert.NotEqual(appointment.Id, rebooked.Id);
    }

    [Fact]
    public async Task ConfirmAppointment_ForUnknownId_Throws()
    {
        using var ctx = _db.CreateContext();
        var service = NewService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ConfirmAppointmentAsync(999_999));
    }
}
