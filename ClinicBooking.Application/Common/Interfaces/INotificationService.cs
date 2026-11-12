using ClinicBooking.Application.Common.DTOs;

namespace ClinicBooking.Application.Common.Interfaces;

/// <summary>Reads and updates the in-app notifications shown to a signed-in user.</summary>
public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(string userId, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

    /// <summary>Marks a single notification as read. Returns false if it does not belong to the user.</summary>
    Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
}
