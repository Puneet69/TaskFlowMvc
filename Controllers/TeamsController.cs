using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;
using TaskFlowMvc.ViewModels;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class TeamsController(ITeamService teamService, IEmailSender emailSender, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var teams = await teamService.GetTeamsForUserAsync(userId);
        var vm = new TeamIndexViewModel
        {
            Teams = teams,
            NewTeam = new TeamCreateInputModel()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewTeam")] TeamCreateInputModel model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var teams = await teamService.GetTeamsForUserAsync(userId);
            return View("Index", new TeamIndexViewModel { Teams = teams, NewTeam = model });
        }

        var result = await teamService.CreateTeamAsync(userId, model.Name, model.Description);
        if (!result.Success || result.TeamId is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.TeamId.Value });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var team = await teamService.GetTeamDetailsAsync(id, userId);
        if (team is null)
        {
            return NotFound();
        }

        var vm = new TeamDetailsViewModel
        {
            Team = team,
            Members = team.Members.OrderBy(m => m.Role).ThenBy(m => m.User?.DisplayName ?? string.Empty).ToList(),
            PendingInvitations = team.Invitations
                .Where(i => !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(i => i.CreatedAt)
                .ToList(),
            InviteModel = new TeamInviteInputModel(),
            WorkloadByUser = await teamService.GetTeamWorkloadAsync(id, userId),
            ActivityLogs = await teamService.GetTeamActivityLogsAsync(id, userId, 100)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(int id, [Bind(Prefix = "InviteModel")] TeamInviteInputModel model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a valid email address.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var inviteResult = await teamService.CreateInvitationAsync(id, model.Email, userId, model.Role);
        if (!inviteResult.Success || string.IsNullOrWhiteSpace(inviteResult.Token))
        {
            TempData["Error"] = inviteResult.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        var inviteUrl = Url.Action(nameof(AcceptInvite), "Teams", new { token = inviteResult.Token }, Request.Scheme);
        if (string.IsNullOrWhiteSpace(inviteUrl))
        {
            TempData["Error"] = "Invite was created, but invite link generation failed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var safeLink = HtmlEncoder.Default.Encode(inviteUrl);
        var safeTeamName = HtmlEncoder.Default.Encode(inviteResult.TeamName ?? "your team");

        if (IsSmtpConfigured())
        {
            await emailSender.SendEmailAsync(
                model.Email.Trim(),
                $"Invitation to join {inviteResult.TeamName ?? "TaskFlow team"}",
                $"You were invited to join <strong>{safeTeamName}</strong> in TaskFlow as <strong>{model.Role}</strong>.<br/>" +
                $"Use this link to accept: <a href='{safeLink}'>Accept invitation</a><br/>" +
                "This invite expires in 7 days.");
            TempData["Success"] = "Invitation sent successfully.";
        }
        else
        {
            TempData["Info"] = $"SMTP is not configured. Share invite link manually: {inviteUrl}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> AcceptInvite(string token)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await teamService.AcceptInvitationAsync(token, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.TeamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeInvite(int invitationId, int teamId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await teamService.RevokeInvitationAsync(invitationId, userId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = teamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int teamId, string memberUserId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await teamService.RemoveMemberAsync(teamId, memberUserId, userId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = teamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int teamId, string memberUserId, TeamMemberRole role)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await teamService.ChangeMemberRoleAsync(teamId, memberUserId, role, userId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = teamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferLeadership(int teamId, string newLeaderUserId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await teamService.TransferLeadershipAsync(teamId, newLeaderUserId, userId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = teamId });
    }

    private string? GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    private bool IsSmtpConfigured()
    {
        var host = configuration["Email:Smtp:Host"];
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var fromEmail = configuration["Email:Smtp:FromEmail"];

        return !string.IsNullOrWhiteSpace(host) &&
               !string.IsNullOrWhiteSpace(username) &&
               !string.IsNullOrWhiteSpace(password) &&
               !string.IsNullOrWhiteSpace(fromEmail);
    }
}
