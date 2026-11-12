using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class PaymentCollectionTests
{
    private readonly TestDatabase _db;

    public PaymentCollectionTests(TestDatabase db) => _db = db;

    private static PaymentService NewService(ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateMapper());

    private static async Task<(ApplicationDbContext Ctx, Appointment Appointment, Payment Payment)>
        SeedBookedAppointmentAsync(TestDatabase db)
    {
        var ctx = db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var appointmentService = new AppointmentService(ctx, TestData.CreateMapper());

        var appointment = await appointmentService.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        var payment = await ctx.Payments.SingleAsync(p => p.AppointmentId == appointment.Id);
        return (ctx, appointment, payment);
    }

    [Fact]
    public async Task MarkAsPaid_OnPendingCashPayment_SetsPaidAndTimestamp()
    {
        var (ctx, _, payment) = await SeedBookedAppointmentAsync(_db);
        using (ctx)
        {
            var service = NewService(ctx);

            await service.MarkAsPaidAsync(payment.Id, "collected at desk");

            var stored = await ctx.Payments.SingleAsync(p => p.Id == payment.Id);
            Assert.Equal(PaymentStatus.Paid, stored.Status);
            Assert.NotNull(stored.PaidAt);
            Assert.Contains("collected at desk", stored.PaymentNotes);
        }
    }

    [Fact]
    public async Task MarkAsPaid_Twice_IsRejected()
    {
        var (ctx, _, payment) = await SeedBookedAppointmentAsync(_db);
        using (ctx)
        {
            var service = NewService(ctx);
            await service.MarkAsPaidAsync(payment.Id);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkAsPaidAsync(payment.Id));
        }
    }

    [Fact]
    public async Task MarkAsPaid_OnNonCashPayment_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);

        // Insert a non-cash pending payment directly (the booking service forbids
        // creating one, so we model a legacy/imported row here).
        var appointment = new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = DateOnly.FromDateTime(TestData.Tomorrow),
            StartTime = TestData.TenAm,
            EndTime = TestData.TenAm.Add(TimeSpan.FromMinutes(30)),
            Status = AppointmentStatus.Confirmed
        };
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();

        var cardPayment = new Payment
        {
            AppointmentId = appointment.Id,
            Amount = 300m,
            Currency = "EGP",
            Method = PaymentMethod.CreditCard,
            Status = PaymentStatus.Pending
        };
        ctx.Payments.Add(cardPayment);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkAsPaidAsync(cardPayment.Id));
    }

    [Fact]
    public async Task MarkAsPaid_ForUnknownPayment_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var service = NewService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkAsPaidAsync(999_999));
    }

    [Fact]
    public async Task RefundPayment_SetsRefundedStatus()
    {
        var (ctx, _, payment) = await SeedBookedAppointmentAsync(_db);
        using (ctx)
        {
            var service = NewService(ctx);
            await service.MarkAsPaidAsync(payment.Id);

            await service.RefundPaymentAsync(payment.Id, "patient did not attend");

            var stored = await ctx.Payments.SingleAsync(p => p.Id == payment.Id);
            Assert.Equal(PaymentStatus.Refunded, stored.Status);
        }
    }
}
