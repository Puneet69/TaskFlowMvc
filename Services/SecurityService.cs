using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public class SecurityService(ApplicationDbContext dbContext) : ISecurityService
{
    public async Task RecordLoginActivityAsync(string? userId, string emailAttempted, LoginActivityType activityType, HttpContext httpContext, string details = "")
    {
        dbContext.LoginActivities.Add(new LoginActivity
        {
            UserId = userId,
            EmailAttempted = emailAttempted ?? string.Empty,
            ActivityType = activityType,
            Details = details,
            IpAddress = GetIp(httpContext),
            UserAgent = GetUserAgent(httpContext),
            OccurredAtUtc = DateTime.UtcNow
        });

        if (!string.IsNullOrWhiteSpace(userId) &&
            (activityType == LoginActivityType.LoginSucceeded || activityType == LoginActivityType.TwoFactorSucceeded))
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is not null)
            {
                user.LastLoginAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<string> CreateDeviceSessionAsync(string userId, HttpContext httpContext)
    {
        var sessionKey = Guid.NewGuid().ToString("N");
        dbContext.DeviceSessions.Add(new DeviceSession
        {
            UserId = userId,
            SessionKey = sessionKey,
            DeviceName = BuildDeviceName(httpContext),
            IpAddress = GetIp(httpContext),
            UserAgent = GetUserAgent(httpContext),
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return sessionKey;
    }

    public async Task<bool> ValidateDeviceSessionAsync(string userId, string sessionKey)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionKey))
        {
            return false;
        }

        return await dbContext.DeviceSessions
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.SessionKey == sessionKey && s.RevokedAtUtc == null);
    }

    public async Task TouchDeviceSessionAsync(string sessionKey, HttpContext httpContext)
    {
        var session = await dbContext.DeviceSessions.FirstOrDefaultAsync(s => s.SessionKey == sessionKey && s.RevokedAtUtc == null);
        if (session is null)
        {
            return;
        }

        session.LastSeenAtUtc = DateTime.UtcNow;
        session.IpAddress = GetIp(httpContext);
        session.UserAgent = GetUserAgent(httpContext);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<DeviceSession>> GetActiveDeviceSessionsAsync(string userId)
    {
        return await dbContext.DeviceSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null)
            .OrderByDescending(s => s.LastSeenAtUtc)
            .ToListAsync();
    }

    public async Task RevokeOtherSessionsAsync(string userId, string currentSessionKey)
    {
        var sessions = await dbContext.DeviceSessions
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.SessionKey != currentSessionKey)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.RevokedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task RevokeSessionAsync(string userId, string sessionKey)
    {
        var session = await dbContext.DeviceSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionKey == sessionKey && s.RevokedAtUtc == null);
        if (session is null)
        {
            return;
        }

        session.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task MarkCurrentSessionLoggedOutAsync(string userId, string? sessionKey)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionKey))
        {
            return;
        }

        var session = await dbContext.DeviceSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.SessionKey == sessionKey && s.RevokedAtUtc == null);
        if (session is null)
        {
            return;
        }

        session.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<LoginActivity>> GetLoginActivityAsync(string userId, int take = 100)
    {
        return await dbContext.LoginActivities
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();
    }

    private static string GetIp(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    private static string GetUserAgent(HttpContext httpContext)
    {
        return httpContext.Request.Headers.UserAgent.ToString();
    }

    private static string BuildDeviceName(HttpContext httpContext)
    {
        var ua = GetUserAgent(httpContext);
        if (string.IsNullOrWhiteSpace(ua))
        {
            return "Unknown device";
        }

        return ua.Length <= 80 ? ua : ua[..80];
    }
}
