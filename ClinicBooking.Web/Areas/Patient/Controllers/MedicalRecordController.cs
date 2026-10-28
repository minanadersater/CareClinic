using ClinicBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicBooking.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Roles = "Patient")]
public class MedicalRecordController : Controller
{
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IPatientService _patientService;

    public MedicalRecordController(IMedicalRecordService medicalRecordService, IPatientService patientService)
    {
        _medicalRecordService = medicalRecordService;
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _patientService.GetPatientByUserIdAsync(userId!);
        if (patient == null) return NotFound("Patient profile not found.");

        var records = await _medicalRecordService.GetRecordsForPatientAsync(patient.Id);
        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var patient = await _patientService.GetPatientByUserIdAsync(userId!);
        if (patient == null) return NotFound("Patient profile not found.");

        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null || record.PatientId != patient.Id) return NotFound();

        return View(record);
    }
}
