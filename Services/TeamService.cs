using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Services;

public class TeamService(ApplicationDbContext dbContext) : ITeamService
{
    public async Task<List<Team>> GetTeamsForUserAsync(string userId)
    {
        return await dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Members.Any(m => m.UserId == userId))
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .OrderBy(t => t.Name)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<List<Team>> GetTeamsForAdminAsync()
    {
        return await dbContext.Teams
            .AsNoTracking()
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .OrderBy(t => t.Name)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<Team?> GetTeamDetailsAsync(int teamId, string userId)
    {
        return await dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Id == teamId && t.Members.Any(m => m.UserId == userId))
            .Include(t => t.Owner)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .Include(t => t.Invitations.Where(i => !i.IsAccepted))
                .ThenInclude(i => i.InvitedBy)
            .Include(t => t.Projects.Where(p => !p.IsArchived))
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, int? TeamId)> CreateTeamAsync(string userId, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (false, "You must be signed in to create a team.", null);
        }

        var trimmedName = (name ?? string.Empty).Trim();
        var trimmedDescription = (description ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (false, "Team name is required.", null);
        }

        var now = DateTime.UtcNow;
        var team = new Team
        {
            Name = trimmedName,
            Description = trimmedDescription,
            OwnerId = userId,
            CreatedAt = now
        };

        dbContext.Teams.Add(team);
        dbContext.TeamMembers.Add(new TeamMember
        {
            Team = team,
            UserId = userId,
            Role = TeamMemberRole.TeamLeader,
            JoinedAt = now
        });

        await dbContext.SaveChangesAsync();
        await AddActivityAsync(team.Id, userId, TeamActivityType.TeamCreated, "Team created.");
        return (true, "Team created successfully.", team.Id);
    }

    public async Task<(bool Success, string Message, string? Token, string? TeamName)> CreateInvitationAsync(int teamId, string email, string invitedByUserId, TeamMemberRole role = TeamMemberRole.TeamMember)
    {
        if (string.IsNullOrWhiteSpace(invitedByUserId))
        {
            return (false, "You are not authorized to invite members.", null, null);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Enter a valid email address.", null, null);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return (false, "Enter a valid email address.", null, null);
        }

        var inviterMembership = await dbContext.TeamMembers
            .Include(m => m.Team)
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == invitedByUserId);
        if (inviterMembership is null)
        {
            return (false, "Team not found.", null, null);
        }

        if (!CanManageTeam(inviterMembership.Role))
        {
            return (false, "Only team leaders can invite members.", null, inviterMembership.Team?.Name);
        }

        var userByEmail = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.NormalizedEmail == normalizedEmail.ToUpperInvariant())
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync();

        if (userByEmail is not null)
        {
            var alreadyMember = await dbContext.TeamMembers
                .AsNoTracking()
                .AnyAsync(m => m.TeamId == teamId && m.UserId == userByEmail.Id);
            if (alreadyMember)
            {
                return (false, "This user is already in your team.", null, inviterMembership.Team?.Name);
            }
        }

        var hasPendingInvite = await dbContext.TeamInvitations
            .AsNoTracking()
            .AnyAsync(i => i.TeamId == teamId && i.Email == normalizedEmail && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow);
        if (hasPendingInvite)
        {
            return (false, "A pending invitation already exists for this email.", null, inviterMembership.Team?.Name);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        dbContext.TeamInvitations.Add(new TeamInvitation
        {
            TeamId = teamId,
            Email = normalizedEmail,
            Token = token,
            InvitedById = invitedByUserId,
            InviteRole = role,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsAccepted = false
        });

        await dbContext.SaveChangesAsync();
        await AddActivityAsync(teamId, invitedByUserId, TeamActivityType.InvitationSent, $"Invitation sent to {normalizedEmail} as {role}.");
        return (true, "Invitation created.", token, inviterMembership.Team?.Name);
    }

    public async Task<(bool Success, string Message, int? TeamId)> AcceptInvitationAsync(string token, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (false, "You must be signed in to accept an invitation.", null);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Invitation token is missing.", null);
        }

        var invitationToken = token.Trim();
        var invitation = await dbContext.TeamInvitations
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == invitationToken);
        if (invitation is null)
        {
            return (false, "Invitation not found.", null);
        }

        if (invitation.IsAccepted)
        {
            return (false, "This invitation has already been used.", invitation.TeamId);
        }

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            return (false, "This invitation has expired.", invitation.TeamId);
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return (false, "Your account email is missing. Update your account email and try again.", null);
        }

        if (!string.Equals(user.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Please sign in with {invitation.Email} to accept this invite.", null);
        }

        var alreadyMember = await dbContext.TeamMembers
            .AnyAsync(m => m.TeamId == invitation.TeamId && m.UserId == userId);
        if (!alreadyMember)
        {
            dbContext.TeamMembers.Add(new TeamMember
            {
                TeamId = invitation.TeamId,
                UserId = userId,
                Role = invitation.InviteRole,
                JoinedAt = DateTime.UtcNow
            });
        }

        invitation.IsAccepted = true;
        invitation.AcceptedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await AddActivityAsync(invitation.TeamId, userId, TeamActivityType.InvitationAccepted, $"{user.Email} joined as {invitation.InviteRole}.");
        return (true, $"You joined {invitation.Team?.Name ?? "the team"}.", invitation.TeamId);
    }

    public async Task<(bool Success, string Message)> RevokeInvitationAsync(int invitationId, string userId)
    {
        var invitation = await dbContext.TeamInvitations.FirstOrDefaultAsync(i => i.Id == invitationId);
        if (invitation is null)
        {
            return (false, "Invitation not found.");
        }

        var membership = await dbContext.TeamMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TeamId == invitation.TeamId && m.UserId == userId);
        if (membership is null || !CanManageTeam(membership.Role))
        {
            return (false, "You are not allowed to revoke invites for this team.");
        }

        if (invitation.IsAccepted)
        {
            return (false, "Accepted invitations cannot be revoked.");
        }

        dbContext.TeamInvitations.Remove(invitation);
        await dbContext.SaveChangesAsync();
        await AddActivityAsync(invitation.TeamId, userId, TeamActivityType.InvitationRevoked, $"Invitation for {invitation.Email} revoked.");
        return (true, "Invitation revoked.");
    }

    public async Task<(bool Success, string Message)> RemoveMemberAsync(int teamId, string memberUserId, string actorUserId)
    {
        var actorMembership = await dbContext.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == actorUserId);
        if (actorMembership is null || !CanManageTeam(actorMembership.Role))
        {
            return (false, "You are not allowed to remove members.");
        }

        if (memberUserId == actorUserId)
        {
            return (false, "Use leadership transfer before leaving the team.");
        }

        var member = await dbContext.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberUserId);
        if (member is null)
        {
            return (false, "Member not found.");
        }

        dbContext.TeamMembers.Remove(member);
        await dbContext.SaveChangesAsync();
        await AddActivityAsync(teamId, actorUserId, TeamActivityType.MemberRemoved, $"User {memberUserId} removed from team.");
        return (true, "Member removed.");
    }

    public async Task<(bool Success, string Message)> ChangeMemberRoleAsync(int teamId, string memberUserId, TeamMemberRole role, string actorUserId)
    {
        var actorMembership = await dbContext.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == actorUserId);
        if (actorMembership is null || !CanManageTeam(actorMembership.Role))
        {
            return (false, "You are not allowed to change member roles.");
        }

        var member = await dbContext.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == memberUserId);
        if (member is null)
        {
            return (false, "Member not found.");
        }

        member.Role = role;
        await dbContext.SaveChangesAsync();
        await AddActivityAsync(teamId, actorUserId, TeamActivityType.LeaderAssigned, $"User {memberUserId} role changed to {role}.");
        return (true, "Member role updated.");
    }

    public async Task<(bool Success, string Message)> TransferLeadershipAsync(int teamId, string newLeaderUserId, string actorUserId)
    {
        var team = await dbContext.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
        {
            return (false, "Team not found.");
        }

        if (team.OwnerId != actorUserId)
        {
            return (false, "Only current team owner can transfer leadership.");
        }

        var newLeaderMembership = await dbContext.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == newLeaderUserId);
        var currentLeaderMembership = await dbContext.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == actorUserId);
        if (newLeaderMembership is null || currentLeaderMembership is null)
        {
            return (false, "Selected leader must be a current team member.");
        }

        team.OwnerId = newLeaderUserId;
        currentLeaderMembership.Role = TeamMemberRole.TeamMember;
        newLeaderMembership.Role = TeamMemberRole.TeamLeader;
        await dbContext.SaveChangesAsync();
        await AddActivityAsync(teamId, actorUserId, TeamActivityType.LeaderTransferred, $"Leadership transferred to {newLeaderUserId}.");
        return (true, "Team leadership transferred.");
    }

    public async Task<Dictionary<string, int>> GetTeamWorkloadAsync(int teamId, string userId)
    {
        var isMember = await dbContext.TeamMembers.AsNoTracking().AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
        if (!isMember)
        {
            return new Dictionary<string, int>();
        }

        var rows = await dbContext.TaskItems
            .AsNoTracking()
            .Where(t => t.Project != null && t.Project.TeamId == teamId && t.Status != TaskStatus.Completed)
            .GroupBy(t => t.AssignedToId ?? "Unassigned")
            .Select(g => new { Assignee = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => r.Assignee, r => r.Count, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<TeamActivityLog>> GetTeamActivityLogsAsync(int teamId, string userId, int take = 50)
    {
        var isMember = await dbContext.TeamMembers.AsNoTracking().AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
        if (!isMember)
        {
            return new List<TeamActivityLog>();
        }

        return await dbContext.TeamActivityLogs
            .AsNoTracking()
            .Where(l => l.TeamId == teamId)
            .Include(l => l.ActorUser)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();
    }

    public async Task<int> GetTeamCountForUserAsync(string userId)
    {
        return await dbContext.TeamMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.TeamId)
            .Distinct()
            .CountAsync();
    }

    private async Task AddActivityAsync(int teamId, string? actorUserId, TeamActivityType type, string description)
    {
        dbContext.TeamActivityLogs.Add(new TeamActivityLog
        {
            TeamId = teamId,
            ActorUserId = actorUserId,
            ActivityType = type,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private static bool CanManageTeam(TeamMemberRole role)
    {
        return role == TeamMemberRole.TeamLeader;
    }
}
