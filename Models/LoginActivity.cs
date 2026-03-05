using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class LoginActivity
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string EmailAttempted { get; set; } = string.Empty;
    public LoginActivityType ActivityType { get; set; }
    public string Details { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
