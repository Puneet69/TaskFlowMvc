using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public class UserAdministrationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IEmailSender emailSender) : IUserAdministrationService
{
    public async Task<List<ApplicationUser>> GetUsersAsync()
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<(bool Success, string Message, string? UserId)> CreateUserAsync(string email, string password, string role, bool emailConfirmed)
    {
        if (!IsValidEmail(email))
        {
            return (false, "Enter a valid email address.", null);
        }

        if (!AppRoles.All.Contains(role))
        {
            return (false, "Invalid role.", null);
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return (false, "A user with this email already exists.", null);
        }

        var user = new ApplicationUser
        {
            Email = email.Trim(),
            UserName = email.Trim(),
            EmailConfirmed = emailConfirmed,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)), null);
        }

        await EnsureRoleAsync(role);
        await userManager.AddToRoleAsync(user, role);
        return (true, "User created successfully.", user.Id);
    }

    public async Task<(bool Success, string Message)> SetUserRoleAsync(string userId, string role)
    {
        if (!AppRoles.All.Contains(role))
        {
            return (false, "Invalid role.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        await EnsureRoleAsync(role);
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await userManager.AddToRoleAsync(user, role);
        return (true, "Role updated.");
    }

    public async Task<(bool Success, string Message)> DisableUserAsync(string userId, string reason)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        user.IsDisabled = true;
        user.DisabledAtUtc = DateTime.UtcNow;
        user.DisabledReason = reason?.Trim() ?? string.Empty;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await userManager.UpdateAsync(user);
        return (true, "User disabled.");
    }

    public async Task<(bool Success, string Message)> EnableUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "User not found.");
        }

        user.IsDisabled = false;
        user.DisabledAtUtc = null;
        user.DisabledReason = string.Empty;
        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);
        return (true, "User enabled.");
    }

    public async Task<(bool Success, string Message, string? Token)> InviteUserAsync(string email, string role, string invitedByUserId)
    {
        if (!IsValidEmail(email))
        {
            return (false, "Enter a valid email address.", null);
        }

        if (!AppRoles.All.Contains(role))
        {
            return (false, "Invalid role.", null);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingPending = await dbContext.UserInvitations
            .AsNoTracking()
            .AnyAsync(i => i.Email == normalizedEmail && !i.IsAccepted && i.ExpiresAtUtc > DateTime.UtcNow);
        if (existingPending)
        {
            return (false, "An active invitation already exists for this email.", null);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        dbContext.UserInvitations.Add(new UserInvitation
        {
            Email = normalizedEmail,
            Role = role,
            Token = token,
            InvitedById = invitedByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await dbContext.SaveChangesAsync();

        await emailSender.SendEmailAsync(
            normalizedEmail,
            "TaskFlow invitation",
            $"You were invited to TaskFlow as <strong>{role}</strong>. " +
            $"Use this link after login/register: /AdminUsers/AcceptInvite?token={token}");

        return (true, "Invitation created.", token);
    }

    public async Task<(bool Success, string Message)> AcceptInviteAsync(string token, string userId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Invalid invite token.");
        }

        var invitation = await dbContext.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == token.Trim().ToLowerInvariant());
        if (invitation is null)
        {
            return (false, "Invitation not found.");
        }

        if (invitation.IsAccepted)
        {
            return (false, "Invitation already accepted.");
        }

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return (false, "Invitation expired.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return (false, "User not found.");
        }

        if (!string.Equals(user.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Sign in with {invitation.Email} to accept this invitation.");
        }

        await EnsureRoleAsync(invitation.Role);
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await userManager.AddToRoleAsync(user, invitation.Role);
        invitation.IsAccepted = true;
        invitation.AcceptedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return (true, "Invitation accepted.");
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static bool IsValidEmail(string email)
    {
        return new EmailAddressAttribute().IsValid(email?.Trim());
    }
}
