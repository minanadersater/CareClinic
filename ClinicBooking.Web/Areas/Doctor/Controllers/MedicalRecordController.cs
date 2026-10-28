using ClinicBooking.Application.Common.DTOs;
using ClinicBooking.Application.Interfaces;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Web.ViewModels.Doctor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Doctor.Controllers;

[Area("Doctor")]
[Authorize(Roles = "Doctor")]
public class MedicalRecordController : Controller
{
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IDoctorService _doctorService;
    private readonly ClinicBooking.Infrastructure.Data.ApplicationDbContext _db;

    public MedicalRecordController(IMedicalRecordService medicalRecordService, IDoctorService doctorService, ClinicBooking.Infrastructure.Data.ApplicationDbContext db)
    {
        _medicalRecordService = medicalRecordService;
        _doctorService = doctorService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var doctor = await _doctorService.GetByUserIdAsync(userId!);
        if (doctor == null) return NotFound("Doctor profile not found.");

        var records = await _medicalRecordService.GetRecordsForDoctorAsync(doctor.Id);
        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null) return NotFound();

        return View(record);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int appointmentId)
    {
        var appointment = await _db.Appointments.FindAsync(appointmentId);
        if (appointment == null) return NotFound();

        var existingRecord = await _medicalRecordService.GetByAppointmentIdAsync(appointmentId);
        if (existingRecord != null)
        {
            return RedirectToAction(nameof(Details), new { id = existingRecord.Id });
        }

        var patient = await _db.Patients.FindAsync(appointment.PatientId);

        var model = new CreateRecordViewModel
        {
            AppointmentId = appointmentId,
            PatientId = appointment.PatientId,
            PatientName = patient?.FullName ?? "Unknown",
            // Add a single empty row for the prescription UI
            Items = new List<PrescriptionItemViewModel> { new PrescriptionItemViewModel() }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRecordViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var doctor = await _doctorService.GetByUserIdAsync(userId!);
        if (doctor == null) return Unauthorized();

        // If prescription is not requested, clear any item validation errors
        if (!model.AddPrescription)
        {
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("Items") || k.StartsWith("Prescription")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        var record = new MedicalRecord
        {
            PatientId = model.PatientId,
            DoctorId = doctor.Id,
            AppointmentId = model.AppointmentId,
            Diagnosis = model.Diagnosis,
            Symptoms = model.Symptoms,
            Notes = model.Notes,
            TreatmentPlan = model.TreatmentPlan
        };

        Prescription? prescription = null;
        if (model.AddPrescription && model.Items.Any(i => !string.IsNullOrWhiteSpace(i.MedicineName)))
        {
            prescription = new Prescription
            {
                Notes = model.PrescriptionNotes,
                Items = model.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.MedicineName))
                    .Select(i => new PrescriptionItem
                    {
                        MedicineName = i.MedicineName,
                        Dosage = i.Dosage,
                        Frequency = i.Frequency,
                        Duration = i.DurationDays > 0 ? $"{i.DurationDays} Days" : "7 Days",
                        Instructions = i.Instructions
                    }).ToList()
            };
        }

        await _medicalRecordService.CreateRecordWithPrescriptionAsync(record, prescription);
        
        // Mark appointment as Completed
        var appointment = await _db.Appointments.FindAsync(model.AppointmentId);
        if (appointment != null)
        {
            appointment.Status = ClinicBooking.Domain.Enums.AppointmentStatus.Completed;
            await _db.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Medical record and prescription saved successfully.";
        return RedirectToAction("Index", "Appointment", new { area = "Doctor" });
    }
}
