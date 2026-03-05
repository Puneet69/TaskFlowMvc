using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class TeamActivityLog
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public string? ActorUserId { get; set; }
    public ApplicationUser? ActorUser { get; set; }

    public TeamActivityType ActivityType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
