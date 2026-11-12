using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class MedicalRecordTests
{
    private readonly TestDatabase _db;

    public MedicalRecordTests(TestDatabase db) => _db = db;

    [Fact]
    public async Task CreateRecordWithPrescription_PersistsRecordAndItems()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);

        var appointmentService = new AppointmentService(ctx, TestData.CreateMapper());
        var appointment = await appointmentService.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);
        await appointmentService.CompleteAppointmentAsync(appointment.Id);

        var service = new MedicalRecordService(ctx, TestData.CreateMapper());

        var record = new MedicalRecord
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentId = appointment.Id,
            Diagnosis = "Influenza",
            Symptoms = "Fever, cough",
            TreatmentPlan = "Rest and fluids"
        };

        var prescription = new Prescription
        {
            Notes = "Take after meals",
            Items =
            {
                new PrescriptionItem
                {
                    MedicineName = "Paracetamol",
                    Dosage = "500mg",
                    Frequency = "3x Day",
                    Duration = "5 Days"
                }
            }
        };

        var created = await service.CreateRecordWithPrescriptionAsync(record, prescription);

        var stored = await ctx.MedicalRecords
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.Items)
            .SingleAsync(m => m.Id == created.Id);

        Assert.Equal("Influenza", stored.Diagnosis);
        var storedPrescription = Assert.Single(stored.Prescriptions);
        Assert.Equal(patient.Id, storedPrescription.PatientId);
        Assert.Equal(doctor.Id, storedPrescription.DoctorId);
        var item = Assert.Single(storedPrescription.Items);
        Assert.Equal("Paracetamol", item.MedicineName);
    }

    [Fact]
    public async Task GetByAppointmentId_ReturnsRecordWithPrescription()
    {
        using var ctx = _db.CreateContext();
        var (doctor, patient) = await TestData.SeedDoctorAndPatientAsync(ctx);

        var appointmentService = new AppointmentService(ctx, TestData.CreateMapper());
        var appointment = await appointmentService.BookAppointmentAsync(
            patient.Id, doctor.Id, TestData.Tomorrow, TestData.TenAm);

        var service = new MedicalRecordService(ctx, TestData.CreateMapper());
        await service.CreateRecordWithPrescriptionAsync(
            new MedicalRecord
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentId = appointment.Id,
                Diagnosis = "Checkup"
            },
            prescription: null);

        var found = await service.GetByAppointmentIdAsync(appointment.Id);

        Assert.NotNull(found);
        Assert.Equal("Checkup", found!.Diagnosis);
        Assert.Empty(found.Prescriptions);
    }
}
