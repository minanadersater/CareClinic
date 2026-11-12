namespace ClinicBooking.Web.ViewModels.Patient;

public class RescheduleAppointmentViewModel
{
    public int AppointmentId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public DateOnly CurrentDate { get; set; }
    public TimeSpan CurrentStartTime { get; set; }

    /// <summary>The date whose availability is currently shown.</summary>
    public DateTime SelectedDate { get; set; }

    public IReadOnlyList<TimeSpan> Slots { get; set; } = new List<TimeSpan>();

    public int CancellationCutoffHours { get; set; }
}
