using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class AppointmentBookingTests
{
    private readonly TestDatabase _db;

    public AppointmentBookingTests(TestDatabase db) => _db = db;

    private AppointmentService NewService(ClinicBooking.Infrastructure.Data.ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateMapper());

    [Fact]
    public async Task BookAppointment_WithValidSlot_CreatesPendingAppointmentAndPendingCashPayment()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var appointment = await service.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm,
            PaymentMethod.Cash, "cash at clinic", null, "checkup");

        Assert.Equal(AppointmentStatus.Pending, appointment.Status);
        Assert.Equal(new TimeSpan(10, 30, 0), appointment.EndTime);

        var stored = await ctx.Appointments
            .Include(a => a.Payment)
            .SingleAsync(a => a.Id == appointment.Id);

        Assert.NotNull(stored.Payment);
        Assert.Equal(PaymentMethod.Cash, stored.Payment!.Method);
        Assert.Equal(PaymentStatus.Pending, stored.Payment.Status);
        Assert.Equal(doctor.ConsultationFee, stored.Payment.Amount);
        Assert.Null(stored.Payment.PaidAt);
    }

    [Fact]
    public async Task BookAppointment_SameSlotTwice_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        var otherPatient = TestData.NewPatient();
        ctx.Patients.Add(otherPatient);
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookAppointmentAsync(otherPatient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm));

        Assert.Contains("already been booked", ex.Message, StringComparison.OrdinalIgnoreCase);

        var count = await ctx.Appointments.CountAsync(a => a.DoctorId == doctor.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BookAppointment_OverlappingSlotWithAnotherDoctor_IsRejectedForSamePatient()
    {
        using var ctx = _db.CreateContext();
        var (doctorA, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var (doctorB, _) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        await service.BookAppointmentAsync(patient.Id, doctorA.Id, TestData.Tomorrow, TestData.TenAm);

        // Different doctor, so the doctor is free - but the patient already has an
        // appointment at this time, which must be rejected.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookAppointmentAsync(patient.Id, doctorB.Id, TestData.Tomorrow, TestData.TenAm));

        Assert.Contains("already have an appointment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BookAppointment_WithElectronicPayment_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookAppointmentAsync(
                patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm, PaymentMethod.CreditCard));
    }

    [Fact]
    public async Task BookAppointment_InThePast_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookAppointmentAsync(
                patient.Id, doctor.Id, DateTime.Today.AddDays(-1), TestData.TenAm));
    }

    [Fact]
    public async Task BookAppointment_OutsideWorkingHours_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        // Schedule starts at 09:00; 08:00 is not a bookable slot.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookAppointmentAsync(
                patient.Id, doctor.Id, TestData.Tomorrow, new TimeSpan(8, 0, 0)));
    }

    [Fact]
    public async Task BookAppointment_ForInactiveDoctor_IsRejected()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        doctor.IsActive = false;
        await ctx.SaveChangesAsync();
        var service = NewService(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm));
    }

    [Fact]
    public async Task GetAvailableSlots_ExcludesAlreadyBookedSlot()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        var before = (await service.GetAvailableSlotsAsync(doctor.Id, TestData.Tomorrow)).ToList();
        Assert.Contains(TestData.TenAm, before);

        await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        var after = (await service.GetAvailableSlotsAsync(doctor.Id, TestData.Tomorrow)).ToList();
        Assert.DoesNotContain(TestData.TenAm, after);
        // 09:00-17:00 with 30-minute slots = 16 slots, one of which is now taken.
        Assert.Equal(15, after.Count);
    }

    [Fact]
    public async Task BookAppointment_SameSlotDifferentDay_IsAllowed()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);
        var service = NewService(ctx);

        await service.BookAppointmentAsync(patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);
        var second = await service.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow.AddDays(1), TestData.TenAm);

        Assert.Equal(AppointmentStatus.Pending, second.Status);
    }
}
