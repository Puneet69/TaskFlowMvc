using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class TeamMember
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public TeamMemberRole Role { get; set; } = TeamMemberRole.TeamMember;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
