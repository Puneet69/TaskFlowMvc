using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskFlowMvc.ViewModels;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Services;

public class ReportService(ApplicationDbContext dbContext) : IReportService
{
    public async Task<ReportsViewModel> GetProjectReportAsync(string userId, DateTime fromDateUtc, DateTime toDateUtc)
    {
        var from = fromDateUtc.Date;
        var to = toDateUtc.Date;
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var projectIds = await GetAccessibleProjectIdsAsync(userId);
        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id) && !p.IsArchived)
            .Include(p => p.Team)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var rows = new List<ReportProjectRowViewModel>();
        var today = DateTime.UtcNow.Date;
        var toExclusive = to.AddDays(1);
        foreach (var project in projects)
        {
            var taskQuery = dbContext.TaskItems
                .AsNoTracking()
                .Where(t => t.ProjectId == project.Id && t.DueDate.Date >= from && t.DueDate.Date <= to);

            var totalTasks = await taskQuery.CountAsync();
            var completedTasks = await taskQuery.CountAsync(t => t.Status == TaskStatus.Completed);
            var pendingTasks = await taskQuery.CountAsync(t => t.Status != TaskStatus.Completed);
            var overdueTasks = await taskQuery.CountAsync(t => t.Status != TaskStatus.Completed && t.DueDate.Date < today);

            var tracked = await dbContext.TimeEntries
                .AsNoTracking()
                .Where(e =>
                    e.TaskItem != null &&
                    e.TaskItem.ProjectId == project.Id &&
                    e.StartedAtUtc >= from &&
                    e.StartedAtUtc < toExclusive)
                .ToListAsync();

            var trackedMinutes = tracked.Sum(e =>
                e.EndedAtUtc.HasValue ? e.MinutesSpent : Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow - e.StartedAtUtc).TotalMinutes)));

            rows.Add(new ReportProjectRowViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                TeamName = project.Team?.Name ?? "Unassigned",
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks,
                TotalTrackedMinutes = trackedMinutes
            });
        }

        return new ReportsViewModel
        {
            FromDateUtc = from,
            ToDateUtc = to,
            Projects = rows
        };
    }

    public async Task<string> BuildProjectReportCsvAsync(string userId, DateTime fromDateUtc, DateTime toDateUtc)
    {
        var report = await GetProjectReportAsync(userId, fromDateUtc, toDateUtc);
        var sb = new StringBuilder();
        sb.AppendLine("ProjectId,ProjectName,Team,TotalTasks,CompletedTasks,PendingTasks,OverdueTasks,TrackedMinutes");

        foreach (var row in report.Projects)
        {
            sb.Append(row.ProjectId).Append(',');
            sb.Append(EscapeCsv(row.ProjectName)).Append(',');
            sb.Append(EscapeCsv(row.TeamName)).Append(',');
            sb.Append(row.TotalTasks).Append(',');
            sb.Append(row.CompletedTasks).Append(',');
            sb.Append(row.PendingTasks).Append(',');
            sb.Append(row.OverdueTasks).Append(',');
            sb.Append(row.TotalTrackedMinutes).AppendLine();
        }

        return sb.ToString();
    }

    private async Task<List<int>> GetAccessibleProjectIdsAsync(string userId)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == userId || (p.Team != null && p.Team.Members.Any(m => m.UserId == userId)))
            .Select(p => p.Id)
            .ToListAsync();
    }

    private static string EscapeCsv(string value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }
}
