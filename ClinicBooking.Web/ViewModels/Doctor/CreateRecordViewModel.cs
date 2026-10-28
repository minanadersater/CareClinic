using System.ComponentModel.DataAnnotations;

namespace ClinicBooking.Web.ViewModels.Doctor;

public class CreateRecordViewModel
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string Diagnosis { get; set; } = string.Empty;
    
    [Required]
    public string Symptoms { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    
    [Required]
    [Display(Name = "Treatment Plan")]
    public string TreatmentPlan { get; set; } = string.Empty;

    public bool AddPrescription { get; set; }
    public string? PrescriptionNotes { get; set; }
    
    public List<PrescriptionItemViewModel> Items { get; set; } = new List<PrescriptionItemViewModel>();
}

public class PrescriptionItemViewModel
{
    [Required]
    public string MedicineName { get; set; } = string.Empty;
    [Required]
    public string Dosage { get; set; } = string.Empty;
    [Required]
    public string Frequency { get; set; } = string.Empty;
    [Required]
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
}

