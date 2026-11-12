using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Data;
using ClinicBooking.Infrastructure.Services;
using ClinicBooking.Tests.Infrastructure;
using Xunit;

namespace ClinicBooking.Tests;

[Collection("Database")]
public class NotificationServiceTests
{
    private readonly TestDatabase _db;

    public NotificationServiceTests(TestDatabase db) => _db = db;

    private static NotificationService NewService(ApplicationDbContext ctx) =>
        new(ctx, TestData.CreateMapper());

    private static Notification NewNotification(string userId, bool isRead = false, int? appointmentId = null) => new()
    {
        UserId = userId,
        Type = NotificationType.AppointmentReminder,
        Title = "Appointment reminder",
        Message = "You have an upcoming appointment.",
        IsRead = isRead,
        AppointmentId = appointmentId
    };

    [Fact]
    public async Task GetUnreadCount_CountsOnlyThatUsersUnreadNotifications()
    {
        using var ctx = _db.CreateContext();
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        ctx.Notifications.AddRange(
            NewNotification(userId),
            NewNotification(userId),
            NewNotification(userId, isRead: true),
            NewNotification(otherUserId));
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);

        Assert.Equal(2, await service.GetUnreadCountAsync(userId));
        Assert.Equal(1, await service.GetUnreadCountAsync(otherUserId));
    }

    [Fact]
    public async Task GetForUser_ReturnsOnlyThatUsersNotificationsNewestFirst()
    {
        using var ctx = _db.CreateContext();
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var older = NewNotification(userId);
        older.CreatedAt = new DateTime(2030, 1, 1, 8, 0, 0);
        var newer = NewNotification(userId);
        newer.CreatedAt = new DateTime(2030, 1, 2, 8, 0, 0);

        ctx.Notifications.AddRange(older, newer, NewNotification(otherUserId));
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.GetForUserAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task MarkAsRead_MarksTheNotificationAndRecordsTheTime()
    {
        using var ctx = _db.CreateContext();
        var userId = Guid.NewGuid().ToString();
        var notification = NewNotification(userId);
        ctx.Notifications.Add(notification);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.MarkAsReadAsync(notification.Id, userId);

        Assert.True(result);
        var stored = await ctx.Notifications.FindAsync(notification.Id);
        Assert.True(stored!.IsRead);
        Assert.NotNull(stored.ReadAt);
    }

    [Fact]
    public async Task MarkAsRead_ForAnotherUsersNotification_ReturnsFalseAndLeavesItUnread()
    {
        using var ctx = _db.CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var attackerId = Guid.NewGuid().ToString();
        var notification = NewNotification(ownerId);
        ctx.Notifications.Add(notification);
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        var result = await service.MarkAsReadAsync(notification.Id, attackerId);

        Assert.False(result);
        var stored = await ctx.Notifications.FindAsync(notification.Id);
        Assert.False(stored!.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_MarksEveryUnreadNotificationForTheUser()
    {
        using var ctx = _db.CreateContext();
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        ctx.Notifications.AddRange(
            NewNotification(userId),
            NewNotification(userId),
            NewNotification(otherUserId));
        await ctx.SaveChangesAsync();

        var service = NewService(ctx);
        await service.MarkAllAsReadAsync(userId);

        Assert.Equal(0, await service.GetUnreadCountAsync(userId));
        // Another user's notifications are untouched.
        Assert.Equal(1, await service.GetUnreadCountAsync(otherUserId));
    }
}
