using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class NotificationItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    [Required]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(500)]
    public string LinkUrl { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
