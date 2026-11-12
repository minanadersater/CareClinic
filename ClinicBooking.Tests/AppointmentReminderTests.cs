using ClinicBooking.Application.Common;
using ClinicBooking.Application.Common.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ClinicBooking.Tests;

/// <summary>Captures emails in memory so tests can assert what would be sent.</summary>
internal sealed class RecordingEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Messages { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        Messages.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

[Collection("Database")]
public class AppointmentReminderTests
{
    private readonly TestDatabase _db;

    public AppointmentReminderTests(TestDatabase db) => _db = db;

    /// <summary>
    /// A fixed future "now" that is unique per test. Using a distinct year keeps each
    /// test's reminder window completely separate from the appointments created by
    /// other tests, which share one database within the collection.
    /// </summary>
    private static DateTime Future(int year) => new(year, 6, 15, 9, 0, 0);

    private static AppointmentReminderService NewService(
        ApplicationDbContext ctx,
        IEmailSender sender,
        int hoursBefore = 24) =>
        new(
            ctx,
            sender,
            Options.Create(new AppointmentReminderOptions
            {
                Enabled = true,
                HoursBeforeAppointment = hoursBefore,
                CheckIntervalMinutes = 30
            }),
            NullLogger<AppointmentReminderService>.Instance);

    private static async Task<Appointment> AddAppointmentAsync(
        ApplicationDbContext ctx,
        Doctor doctor,
        Patient patient,
        DateTime start,
        AppointmentStatus status)
    {
        var appointment = new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = DateOnly.FromDateTime(start),
            StartTime = start.TimeOfDay,
            EndTime = start.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
            Status = status
        };

        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();
        return appointment;
    }

    [Fact]
    public async Task SendDueReminders_ForUpcomingAppointment_EmailsPatientAndCreatesNotification()
    {
        var now = Future(2030);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var appointment = await AddAppointmentAsync(
            ctx, doctor, patient, now.AddHours(3), AppointmentStatus.Confirmed);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        var sent = await service.SendDueRemindersAsync(now);

        Assert.Equal(1, sent);
        var message = Assert.Single(sender.Messages);
        Assert.Equal(patient.Email, message.To);

        var notification = await ctx.Notifications.SingleAsync(n => n.AppointmentId == appointment.Id);
        Assert.Equal(NotificationType.AppointmentReminder, notification.Type);
        Assert.Equal(patient.UserId, notification.UserId);
        Assert.False(notification.IsRead);
        Assert.Contains(doctor.FullName, notification.Message);
    }

    [Fact]
    public async Task SendDueReminders_RunTwice_SendsOnlyOneReminderPerAppointment()
    {
        var now = Future(2031);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var appointment = await AddAppointmentAsync(
            ctx, doctor, patient, now.AddHours(2), AppointmentStatus.Confirmed);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        Assert.Equal(1, await service.SendDueRemindersAsync(now));
        Assert.Equal(0, await service.SendDueRemindersAsync(now));

        Assert.Single(sender.Messages);
        var count = await ctx.Notifications.CountAsync(n => n.AppointmentId == appointment.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SendDueReminders_ForPendingAppointment_IsAlsoSent()
    {
        var now = Future(2032);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        await AddAppointmentAsync(ctx, doctor, patient, now.AddHours(5), AppointmentStatus.Pending);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        Assert.Equal(1, await service.SendDueRemindersAsync(now));
        Assert.Single(sender.Messages);
    }

    [Fact]
    public async Task SendDueReminders_ForCancelledAppointment_IsSkipped()
    {
        var now = Future(2033);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        await AddAppointmentAsync(ctx, doctor, patient, now.AddHours(3), AppointmentStatus.Cancelled);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        Assert.Equal(0, await service.SendDueRemindersAsync(now));
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task SendDueReminders_ForCompletedAppointment_IsSkipped()
    {
        var now = Future(2034);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        await AddAppointmentAsync(ctx, doctor, patient, now.AddHours(3), AppointmentStatus.Completed);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        Assert.Equal(0, await service.SendDueRemindersAsync(now));
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task SendDueReminders_ForAppointmentOutsideWindow_IsSkipped()
    {
        var now = Future(2035);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        // 48 hours ahead, default window is 24 hours.
        await AddAppointmentAsync(ctx, doctor, patient, now.AddHours(48), AppointmentStatus.Confirmed);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        Assert.Equal(0, await service.SendDueRemindersAsync(now));
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task SendDueReminders_ForPastAppointment_IsSkipped()
    {
        var now = Future(2036);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        await AddAppointmentAsync(ctx, doctor, patient, now.AddHours(-1), AppointmentStatus.Confirmed);

        var sender = new RecordingEmailSender();
        var service = NewService(ctx, sender);

        Assert.Equal(0, await service.SendDueRemindersAsync(now));
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task SendDueReminders_WhenDisabled_SendsNothing()
    {
        var now = Future(2037);
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        await AddAppointmentAsync(ctx, doctor, patient, now.AddHours(3), AppointmentStatus.Confirmed);

        var sender = new RecordingEmailSender();
        var service = new AppointmentReminderService(
            ctx,
            sender,
            Options.Create(new AppointmentReminderOptions { Enabled = false }),
            NullLogger<AppointmentReminderService>.Instance);

        Assert.Equal(0, await service.SendDueRemindersAsync(now));
        Assert.Empty(sender.Messages);
    }
}
