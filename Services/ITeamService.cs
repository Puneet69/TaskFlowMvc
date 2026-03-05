using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public interface ITeamService
{
    Task<List<Team>> GetTeamsForUserAsync(string userId);
    Task<List<Team>> GetTeamsForAdminAsync();
    Task<Team?> GetTeamDetailsAsync(int teamId, string userId);
    Task<(bool Success, string Message, int? TeamId)> CreateTeamAsync(string userId, string name, string description);
    Task<(bool Success, string Message, string? Token, string? TeamName)> CreateInvitationAsync(int teamId, string email, string invitedByUserId, TeamMemberRole role = TeamMemberRole.TeamMember);
    Task<(bool Success, string Message, int? TeamId)> AcceptInvitationAsync(string token, string userId);
    Task<(bool Success, string Message)> RevokeInvitationAsync(int invitationId, string userId);
    Task<(bool Success, string Message)> RemoveMemberAsync(int teamId, string memberUserId, string actorUserId);
    Task<(bool Success, string Message)> ChangeMemberRoleAsync(int teamId, string memberUserId, TeamMemberRole role, string actorUserId);
    Task<(bool Success, string Message)> TransferLeadershipAsync(int teamId, string newLeaderUserId, string actorUserId);
    Task<Dictionary<string, int>> GetTeamWorkloadAsync(int teamId, string userId);
    Task<List<TeamActivityLog>> GetTeamActivityLogsAsync(int teamId, string userId, int take = 50);
    Task<int> GetTeamCountForUserAsync(string userId);
}
