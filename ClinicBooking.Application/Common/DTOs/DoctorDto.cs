namespace ClinicBooking.Application.Common.DTOs;

public class DoctorDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int AppointmentDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public List<string> Specialties { get; set; } = new();
    public double? AverageRating { get; set; }
}

public class CreateDoctorDto
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public int AppointmentDurationMinutes { get; set; } = 30;
    public List<int> SpecialtyIds { get; set; } = new();
}

public class UpdateDoctorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public int AppointmentDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public List<int> SpecialtyIds { get; set; } = new();
}

