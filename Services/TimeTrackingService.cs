using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Services;

public class TimeTrackingService(ApplicationDbContext dbContext) : ITimeTrackingService
{
    public async Task<bool> StartTimerAsync(int taskId, string userId, string? note = null)
    {
        var task = await dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Project)
                .ThenInclude(p => p!.Team)
                    .ThenInclude(team => team!.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || !CanAccessTask(task, userId))
        {
            return false;
        }

        var hasActiveForTask = await dbContext.TimeEntries
            .AnyAsync(e => e.TaskItemId == taskId && e.UserId == userId && e.EndedAtUtc == null);
        if (hasActiveForTask)
        {
            return true;
        }

        var activeEntry = await dbContext.TimeEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EndedAtUtc == null);
        if (activeEntry is not null)
        {
            activeEntry.EndedAtUtc = DateTime.UtcNow;
            activeEntry.MinutesSpent = CalculateMinutes(activeEntry.StartedAtUtc, activeEntry.EndedAtUtc.Value);
        }

        dbContext.TimeEntries.Add(new TimeEntry
        {
            TaskItemId = taskId,
            UserId = userId,
            StartedAtUtc = DateTime.UtcNow,
            EndedAtUtc = null,
            MinutesSpent = 0,
            Note = (note ?? string.Empty).Trim(),
            IsManualEntry = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> StopTimerAsync(int taskId, string userId, string? note = null)
    {
        var entry = await dbContext.TimeEntries
            .Include(e => e.TaskItem)
                .ThenInclude(t => t!.Project)
                    .ThenInclude(p => p!.Team)
                        .ThenInclude(team => team!.Members)
            .FirstOrDefaultAsync(e => e.TaskItemId == taskId && e.UserId == userId && e.EndedAtUtc == null);
        if (entry is null || entry.TaskItem is null || !CanAccessTask(entry.TaskItem, userId))
        {
            return false;
        }

        entry.EndedAtUtc = DateTime.UtcNow;
        entry.MinutesSpent = CalculateMinutes(entry.StartedAtUtc, entry.EndedAtUtc.Value);
        if (!string.IsNullOrWhiteSpace(note))
        {
            entry.Note = note.Trim();
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<TimeEntry?> LogManualAsync(int taskId, string userId, int minutes, string? note = null, DateTime? startAtUtc = null)
    {
        var task = await dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Project)
                .ThenInclude(p => p!.Team)
                    .ThenInclude(team => team!.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || !CanAccessTask(task, userId))
        {
            return null;
        }

        var clampedMinutes = Math.Clamp(minutes, 1, 24 * 60);
        var started = (startAtUtc ?? DateTime.UtcNow).ToUniversalTime();
        var ended = started.AddMinutes(clampedMinutes);

        var entry = new TimeEntry
        {
            TaskItemId = taskId,
            UserId = userId,
            StartedAtUtc = started,
            EndedAtUtc = ended,
            MinutesSpent = clampedMinutes,
            Note = (note ?? string.Empty).Trim(),
            IsManualEntry = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.TimeEntries.Add(entry);
        await dbContext.SaveChangesAsync();
        return entry;
    }

    public async Task<Dictionary<int, int>> GetTotalMinutesByTaskAsync(IEnumerable<int> taskIds, string userId)
    {
        var ids = taskIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var accessibleTaskIds = await GetAccessibleTaskIdsAsync(ids, userId);
        if (accessibleTaskIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var entries = await dbContext.TimeEntries
            .AsNoTracking()
            .Where(e => accessibleTaskIds.Contains(e.TaskItemId))
            .ToListAsync();

        return entries
            .GroupBy(e => e.TaskItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.EndedAtUtc.HasValue ? e.MinutesSpent : CalculateMinutes(e.StartedAtUtc, DateTime.UtcNow)));
    }

    public async Task<HashSet<int>> GetActiveTaskIdsAsync(IEnumerable<int> taskIds, string userId)
    {
        var ids = taskIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var accessibleTaskIds = await GetAccessibleTaskIdsAsync(ids, userId);
        if (accessibleTaskIds.Count == 0)
        {
            return new HashSet<int>();
        }

        var active = await dbContext.TimeEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.EndedAtUtc == null && accessibleTaskIds.Contains(e.TaskItemId))
            .Select(e => e.TaskItemId)
            .Distinct()
            .ToListAsync();

        return active.ToHashSet();
    }

    public async Task<List<TimeEntry>> GetRecentByTaskAsync(int taskId, string userId, int take = 20)
    {
        var accessibleTaskIds = await GetAccessibleTaskIdsAsync([taskId], userId);
        if (!accessibleTaskIds.Contains(taskId))
        {
            return [];
        }

        return await dbContext.TimeEntries
            .AsNoTracking()
            .Where(e => e.TaskItemId == taskId)
            .OrderByDescending(e => e.StartedAtUtc)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();
    }

    private async Task<List<int>> GetAccessibleTaskIdsAsync(List<int> taskIds, string userId)
    {
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(t =>
                taskIds.Contains(t.Id) &&
                (t.Project != null && (t.Project.OwnerId == userId ||
                                       (t.Project.Team != null && t.Project.Team.Members.Any(m => m.UserId == userId)))))
            .Select(t => t.Id)
            .ToListAsync();
    }

    private static bool CanAccessTask(TaskItem task, string userId)
    {
        return task.Project is not null &&
               (task.Project.OwnerId == userId ||
                (task.Project.Team is not null && task.Project.Team.Members.Any(m => m.UserId == userId)));
    }

    private static int CalculateMinutes(DateTime startedUtc, DateTime endedUtc)
    {
        return Math.Max(1, (int)Math.Ceiling((endedUtc - startedUtc).TotalMinutes));
    }
}
