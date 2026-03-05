using Microsoft.AspNetCore.Identity;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Data;

public class ApplicationUser : IdentityUser
{
    public bool IsDisabled { get; set; }
    public DateTime? DisabledAtUtc { get; set; }
    public string DisabledReason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public List<Project> OwnedProjects { get; set; } = new();
    public List<TaskItem> AssignedTasks { get; set; } = new();
    public List<Team> OwnedTeams { get; set; } = new();
    public List<TeamMember> TeamMemberships { get; set; } = new();
    public List<TeamInvitation> SentTeamInvitations { get; set; } = new();
    public List<LoginActivity> LoginActivities { get; set; } = new();
    public List<DeviceSession> DeviceSessions { get; set; } = new();
    public List<NotificationItem> Notifications { get; set; } = new();
    public List<UserInvitation> SentUserInvitations { get; set; } = new();
    public List<TeamActivityLog> TeamActivities { get; set; } = new();
    public List<TaskComment> TaskComments { get; set; } = new();
    public List<FileAttachment> UploadedFiles { get; set; } = new();
    public List<TimeEntry> TimeEntries { get; set; } = new();
}
