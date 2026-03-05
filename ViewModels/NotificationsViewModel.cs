using TaskFlowMvc.Models;

namespace TaskFlowMvc.ViewModels;

public class NotificationsViewModel
{
    public List<NotificationItem> Unread { get; set; } = new();
    public List<NotificationItem> Recent { get; set; } = new();
}
