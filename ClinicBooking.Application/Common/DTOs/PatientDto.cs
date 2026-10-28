namespace ClinicBooking.Application.Common.DTOs;

public class PatientDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int AppointmentCount { get; set; }
}

public class UpdatePatientDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

