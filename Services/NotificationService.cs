using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using TaskFlowMvc.Data;
using TaskFlowMvc.Hubs;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public class NotificationService(
    ApplicationDbContext dbContext,
    IEmailSender emailSender,
    IHubContext<NotificationsHub> notificationsHub,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(string userId, NotificationType type, string title, string message, string linkUrl = "", bool sendEmail = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var inApp = new NotificationItem
        {
            UserId = userId,
            Type = type,
            Channel = NotificationChannel.InApp,
            Title = title,
            Message = message,
            LinkUrl = linkUrl ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.NotificationItems.Add(inApp);
        await dbContext.SaveChangesAsync();
        await BroadcastNotificationAsync(inApp);

        if (!sendEmail)
        {
            return;
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        try
        {
            await emailSender.SendEmailAsync(user.Email, title, $"{message}<br/><a href='{linkUrl}'>Open</a>");
            dbContext.NotificationItems.Add(new NotificationItem
            {
                UserId = userId,
                Type = type,
                Channel = NotificationChannel.Email,
                Title = title,
                Message = message,
                LinkUrl = linkUrl ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send notification email to {UserId}", userId);
        }
    }

    public async Task<List<NotificationItem>> GetUnreadAsync(string userId, int take = 50)
    {
        return await dbContext.NotificationItems
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead && n.Channel == NotificationChannel.InApp)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        return await dbContext.NotificationItems
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.Channel == NotificationChannel.InApp);
    }

    public async Task<List<NotificationItem>> GetRecentAsync(string userId, int take = 100)
    {
        return await dbContext.NotificationItems
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Channel == NotificationChannel.InApp)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(string userId, int notificationId)
    {
        var item = await dbContext.NotificationItems
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Id == notificationId && n.Channel == NotificationChannel.InApp);
        if (item is null)
        {
            return;
        }

        item.IsRead = true;
        item.ReadAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await BroadcastUnreadCountAsync(userId);
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var items = await dbContext.NotificationItems
            .Where(n => n.UserId == userId && !n.IsRead && n.Channel == NotificationChannel.InApp)
            .ToListAsync();

        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        await BroadcastUnreadCountAsync(userId);
    }

    private async Task BroadcastNotificationAsync(NotificationItem item)
    {
        try
        {
            var unreadCount = await GetUnreadCountAsync(item.UserId);
            await notificationsHub.Clients.User(item.UserId).SendAsync("notificationReceived", new
            {
                id = item.Id,
                title = item.Title,
                message = item.Message,
                linkUrl = item.LinkUrl,
                createdAtUtc = item.CreatedAtUtc,
                unreadCount
            });
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to broadcast notification to user {UserId}", item.UserId);
        }
    }

    private async Task BroadcastUnreadCountAsync(string userId)
    {
        try
        {
            var unreadCount = await GetUnreadCountAsync(userId);
            await notificationsHub.Clients.User(userId).SendAsync("notificationCountUpdated", new { unreadCount });
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to broadcast unread count to user {UserId}", userId);
        }
    }
}
