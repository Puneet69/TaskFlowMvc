using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public interface ITimeTrackingService
{
    Task<bool> StartTimerAsync(int taskId, string userId, string? note = null);
    Task<bool> StopTimerAsync(int taskId, string userId, string? note = null);
    Task<TimeEntry?> LogManualAsync(int taskId, string userId, int minutes, string? note = null, DateTime? startAtUtc = null);
    Task<Dictionary<int, int>> GetTotalMinutesByTaskAsync(IEnumerable<int> taskIds, string userId);
    Task<HashSet<int>> GetActiveTaskIdsAsync(IEnumerable<int> taskIds, string userId);
    Task<List<TimeEntry>> GetRecentByTaskAsync(int taskId, string userId, int take = 20);
}
