using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Common.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? AppointmentId { get; set; }
    public DateTime CreatedAt { get; set; }
}
