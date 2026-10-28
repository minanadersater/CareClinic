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
    
    [Range(10, 120, ErrorMessage = "Slot duration must be between 10 and 120 minutes.")]
    public int SlotDurationMinutes { get; set; } = 30;
}

public class ManageScheduleViewModel
{
    public List<ScheduleItemViewModel> Schedules { get; set; } = new();
}

