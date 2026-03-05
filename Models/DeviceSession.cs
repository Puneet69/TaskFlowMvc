using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class DeviceSession
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string SessionKey { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;
}
