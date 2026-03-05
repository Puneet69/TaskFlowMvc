using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public interface ISecurityService
{
    Task RecordLoginActivityAsync(string? userId, string emailAttempted, LoginActivityType activityType, HttpContext httpContext, string details = "");
    Task<string> CreateDeviceSessionAsync(string userId, HttpContext httpContext);
    Task<bool> ValidateDeviceSessionAsync(string userId, string sessionKey);
    Task TouchDeviceSessionAsync(string sessionKey, HttpContext httpContext);
    Task<List<DeviceSession>> GetActiveDeviceSessionsAsync(string userId);
    Task RevokeOtherSessionsAsync(string userId, string currentSessionKey);
    Task RevokeSessionAsync(string userId, string sessionKey);
    Task MarkCurrentSessionLoggedOutAsync(string userId, string? sessionKey);
    Task<List<LoginActivity>> GetLoginActivityAsync(string userId, int take = 100);
}
