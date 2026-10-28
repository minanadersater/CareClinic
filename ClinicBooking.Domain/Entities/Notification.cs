using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = default!; // FK -> AspNetUsers.Id (recipient)

    public NotificationType Type { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    /// <summary>Optional link to the related appointment, if applicable.</summary>
    public int? AppointmentId { get; set; }
}
