using TaskFlowMvc.Models;

namespace TaskFlowMvc.ViewModels;

public class TeamDetailsViewModel
{
    public Team Team { get; set; } = new();
    public List<TeamMember> Members { get; set; } = new();
    public List<TeamInvitation> PendingInvitations { get; set; } = new();
    public TeamInviteInputModel InviteModel { get; set; } = new();
    public Dictionary<string, int> WorkloadByUser { get; set; } = new();
    public List<TeamActivityLog> ActivityLogs { get; set; } = new();
}
