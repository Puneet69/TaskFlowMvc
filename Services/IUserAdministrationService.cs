using TaskFlowMvc.Data;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public interface IUserAdministrationService
{
    Task<List<ApplicationUser>> GetUsersAsync();
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<(bool Success, string Message, string? UserId)> CreateUserAsync(string email, string password, string role, bool emailConfirmed);
    Task<(bool Success, string Message)> SetUserRoleAsync(string userId, string role);
    Task<(bool Success, string Message)> DisableUserAsync(string userId, string reason);
    Task<(bool Success, string Message)> EnableUserAsync(string userId);
    Task<(bool Success, string Message, string? Token)> InviteUserAsync(string email, string role, string invitedByUserId);
    Task<(bool Success, string Message)> AcceptInviteAsync(string token, string userId);
}
