using System.ComponentModel.DataAnnotations;

namespace ClinicBooking.Web.ViewModels.Doctor;

public class ScheduleItemViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    
    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }
    
    [DataType(DataType.Time)]
    public TimeSpan? EndTime { get; set; }
    
}

public class ManageScheduleViewModel
{
    public List<ScheduleItemViewModel> Schedules { get; set; } = new();

    [Range(10, 120, ErrorMessage = "Appointment duration must be between 10 and 120 minutes.")]
    [Display(Name = "Appointment duration (minutes)")]
    public int AppointmentDurationMinutes { get; set; } = 30;
}

