using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Common.DTOs;

public class DoctorScheduleDto
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public WeekDay DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertScheduleDto
{
    public int? Id { get; set; }
    public int DoctorId { get; set; }
    public WeekDay DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MedicalRecordDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = default!;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = default!;
    public int? AppointmentId { get; set; }
    public string? Diagnosis { get; set; }
    public string? Symptoms { get; set; }
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
    public DateTime RecordDate { get; set; }
}

public class CreateMedicalRecordDto
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    public string? Diagnosis { get; set; }
    public string? Symptoms { get; set; }
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
}

public class SpecialtyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

