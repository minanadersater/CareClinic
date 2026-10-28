namespace ClinicBooking.Application.DTOs;

public class AdminDashboardDto
{
    public int TotalDoctors { get; set; }
    public int ActiveDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TodaysAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
}

public class DoctorDashboardDto
{
    public int TodaysAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int TotalPatients { get; set; }
}

public class PatientDashboardDto
{
    public int TotalAppointments { get; set; }
    public int MedicalRecordsCount { get; set; }
}

