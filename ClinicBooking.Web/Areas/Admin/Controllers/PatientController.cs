using ClinicBooking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdmin")]
public class PatientController : Controller
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
        => _patientService = patientService;

    public async Task<IActionResult> Index()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        return View(patients);
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient is null) return NotFound();
        return View(patient);
    }
}

