using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public interface INotificationService
{
    Task NotifyAsync(string userId, NotificationType type, string title, string message, string linkUrl = "", bool sendEmail = false);
    Task<List<NotificationItem>> GetUnreadAsync(string userId, int take = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task<List<NotificationItem>> GetRecentAsync(string userId, int take = 100);
    Task MarkAsReadAsync(string userId, int notificationId);
    Task MarkAllAsReadAsync(string userId);
}
