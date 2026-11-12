using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

public class AppointmentPolicyTests
{
    [Fact]
    public void IsWithinChangeWindow_IsTrueWhenFarEnoughAhead()
    {
        var date = new DateOnly(2026, 1, 1);
        var start = new TimeSpan(12, 0, 0);
        var now = new DateTime(2026, 1, 1, 9, 0, 0);

        // 3 hours ahead, 2-hour cutoff -> still allowed.
        Assert.True(AppointmentPolicy.IsWithinChangeWindow(date, start, now, 2));
    }

    [Fact]
    public void IsWithinChangeWindow_IsFalseWhenTooClose()
    {
        var date = new DateOnly(2026, 1, 1);
        var start = new TimeSpan(12, 0, 0);
        var now = new DateTime(2026, 1, 1, 11, 0, 0);

        // 1 hour ahead, 2-hour cutoff -> closed.
        Assert.False(AppointmentPolicy.IsWithinChangeWindow(date, start, now, 2));
    }

    [Fact]
    public void IsWithinChangeWindow_WithZeroCutoff_IsOpenUntilStart()
    {
        var date = new DateOnly(2026, 1, 1);
        var start = new TimeSpan(12, 0, 0);

        Assert.True(AppointmentPolicy.IsWithinChangeWindow(date, start, new DateTime(2026, 1, 1, 12, 0, 0), 0));
        Assert.False(AppointmentPolicy.IsWithinChangeWindow(date, start, new DateTime(2026, 1, 1, 12, 0, 1), 0));
    }
}

[Collection("Database")]
public class AppointmentRescheduleTests
{
    private const int CutoffHours = 2;
    private const int ClosedCutoffHours = 1000; // pushes "tomorrow" outside the window

    private readonly TestDatabase _db;

    public AppointmentRescheduleTests(TestDatabase db) => _db = db;

    private static AppointmentService NewService(ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateMapper());

    [Fact]
    public async Task CancelByPatient_WithinWindow_CancelsAppointment()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);
        var appt = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        await service.CancelByPatientAsync(
            new CancelAppointmentDto { AppointmentId = appt.Id, CancellationReason = "Patient request" },
            CutoffHours);

        var stored = await ctx.Appointments.SingleAsync(a => a.Id == appt.Id);
        Assert.Equal(AppointmentStatus.Cancelled, stored.Status);
        Assert.Equal("Patient request", stored.CancellationReason);
    }

    [Fact]
    public async Task CancelByPatient_OutsideWindow_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);
        var appt = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelByPatientAsync(new CancelAppointmentDto { AppointmentId = appt.Id }, ClosedCutoffHours));

        var stored = await ctx.Appointments.SingleAsync(a => a.Id == appt.Id);
        Assert.Equal(AppointmentStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task Reschedule_WithinWindow_MovesAppointmentAndResetsToPending()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var appt = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);
        await service.ConfirmAppointmentAsync(appt.Id);

        var newDate = DateOnly.FromDateTime(TestData.Tomorrow.AddDays(1));
        var moved = await service.RescheduleAppointmentAsync(new RescheduleAppointmentDto
        {
            AppointmentId = appt.Id,
            AppointmentDate = newDate,
            StartTime = new TimeSpan(11, 0, 0)
        }, CutoffHours);

        Assert.Equal(newDate, moved.AppointmentDate);
        Assert.Equal(new TimeSpan(11, 0, 0), moved.StartTime);
        Assert.Equal(new TimeSpan(11, 30, 0), moved.EndTime);
        // A moved appointment must be re-confirmed by the clinic.
        Assert.Equal(AppointmentStatus.Pending, moved.Status);
    }

    [Fact]
    public async Task Reschedule_OutsideWindow_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);
        var appt = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RescheduleAppointmentAsync(new RescheduleAppointmentDto
            {
                AppointmentId = appt.Id,
                AppointmentDate = DateOnly.FromDateTime(TestData.Tomorrow.AddDays(1)),
                StartTime = new TimeSpan(11, 0, 0)
            }, ClosedCutoffHours));
    }

    [Fact]
    public async Task Reschedule_ToAlreadyBookedSlot_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var mine = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        var otherPatient = TestData.NewPatient();
        ctx.Patients.Add(otherPatient);
        await ctx.SaveChangesAsync();
        await service.BookAppointmentAsync(otherPatient.Id, doctor.Id, TestData.Tomorrow, new TimeSpan(11, 0, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RescheduleAppointmentAsync(new RescheduleAppointmentDto
            {
                AppointmentId = mine.Id,
                AppointmentDate = DateOnly.FromDateTime(TestData.Tomorrow),
                StartTime = new TimeSpan(11, 0, 0)
            }, CutoffHours));
    }

    [Fact]
    public async Task Reschedule_KeepsTheExistingPayment()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var appt = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);
        var paymentBefore = await ctx.Payments.SingleAsync(p => p.AppointmentId == appt.Id);

        await service.RescheduleAppointmentAsync(new RescheduleAppointmentDto
        {
            AppointmentId = appt.Id,
            AppointmentDate = DateOnly.FromDateTime(TestData.Tomorrow.AddDays(1)),
            StartTime = new TimeSpan(11, 0, 0)
        }, CutoffHours);

        var paymentAfter = await ctx.Payments.SingleAsync(p => p.AppointmentId == appt.Id);
        Assert.Equal(paymentBefore.Id, paymentAfter.Id);
        Assert.Equal(paymentBefore.Amount, paymentAfter.Amount);
        Assert.Equal(PaymentStatus.Pending, paymentAfter.Status);
    }

    [Fact]
    public async Task CanPatientModify_IsFalseForCancelledAppointment()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);
        var appt = await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        Assert.True(await service.CanPatientModifyAsync(appt.Id, CutoffHours));

        await service.CancelByPatientAsync(new CancelAppointmentDto { AppointmentId = appt.Id }, CutoffHours);

        Assert.False(await service.CanPatientModifyAsync(appt.Id, CutoffHours));
    }
}
