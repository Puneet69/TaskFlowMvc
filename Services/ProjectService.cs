using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskFlowMvc.ViewModels;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Services;

public class ProjectService(ApplicationDbContext dbContext) : IProjectService
{
    public async Task<List<Project>> GetProjectsAsync(string userId)
    {
        return await AccessibleProjects(userId)
            .AsNoTracking()
            .Where(p => !p.IsArchived)
            .Include(p => p.Team)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Project>> GetProjectsForTeamAsync(int teamId, string userId)
    {
        return await AccessibleProjects(userId)
            .AsNoTracking()
            .Where(p => p.TeamId == teamId && !p.IsArchived)
            .Include(p => p.Team)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectAsync(int id, string userId)
    {
        return await AccessibleProjects(userId)
            .AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.Milestones.OrderBy(m => m.DueDate))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateAsync(Project project, string userId)
    {
        project.OwnerId = userId;
        project.CreatedAt = DateTime.UtcNow;
        project.Status = project.Status == ProjectStatus.Archived ? ProjectStatus.Planning : project.Status;
        project.IsArchived = false;
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();
        return project;
    }

    public async Task<bool> UpdateAsync(Project project, string userId)
    {
        var existing = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id && p.OwnerId == userId);
        if (existing is null)
        {
            return false;
        }

        existing.Name = project.Name;
        existing.Description = project.Description;
        existing.Status = project.Status;
        existing.TeamId = project.TeamId;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var existing = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);
        if (existing is null)
        {
            return false;
        }

        dbContext.Projects.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveAsync(int id, string userId)
    {
        var existing = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);
        if (existing is null)
        {
            return false;
        }

        existing.IsArchived = true;
        existing.ArchivedAtUtc = DateTime.UtcNow;
        existing.Status = ProjectStatus.Archived;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnarchiveAsync(int id, string userId)
    {
        var existing = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);
        if (existing is null)
        {
            return false;
        }

        existing.IsArchived = false;
        existing.ArchivedAtUtc = null;
        if (existing.Status == ProjectStatus.Archived)
        {
            existing.Status = ProjectStatus.Active;
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTeamAsync(int projectId, int? teamId, string userId)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
        if (project is null)
        {
            return false;
        }

        if (teamId.HasValue)
        {
            var isMember = await dbContext.TeamMembers
                .AsNoTracking()
                .AnyAsync(m => m.TeamId == teamId.Value && m.UserId == userId);
            if (!isMember)
            {
                return false;
            }
        }

        project.TeamId = teamId;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<ProjectMilestone?> AddMilestoneAsync(int projectId, ProjectMilestone milestone, string userId)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
        if (project is null)
        {
            return null;
        }

        milestone.ProjectId = projectId;
        milestone.CreatedAtUtc = DateTime.UtcNow;
        dbContext.ProjectMilestones.Add(milestone);
        await dbContext.SaveChangesAsync();
        return milestone;
    }

    public async Task<bool> CompleteMilestoneAsync(int milestoneId, string userId)
    {
        var milestone = await dbContext.ProjectMilestones
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.Id == milestoneId && m.Project != null && m.Project.OwnerId == userId);
        if (milestone is null)
        {
            return false;
        }

        milestone.IsCompleted = true;
        milestone.CompletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<(DateTime Date, string Type, string Title, string Description)>> GetProjectTimelineAsync(int projectId, string userId)
    {
        var project = await AccessibleProjects(userId).AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
        {
            return new List<(DateTime Date, string Type, string Title, string Description)>();
        }

        var milestones = await dbContext.ProjectMilestones
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .Select(m => new { Date = m.DueDate, Type = "Milestone", Title = m.Title, Description = m.Description })
            .ToListAsync();

        var tasks = await dbContext.TaskItems
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Select(t => new { Date = t.DueDate, Type = "Task", Title = t.Title, Description = t.Description })
            .ToListAsync();

        return milestones
            .Concat(tasks)
            .Select(i => (i.Date, i.Type, i.Title, i.Description))
            .OrderBy(i => i.Date)
            .ToList();
    }

    public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(string userId)
    {
        var projectIds = await AccessibleProjects(userId)
            .AsNoTracking()
            .Where(p => !p.IsArchived)
            .Select(p => p.Id)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var dueSoonDate = today.AddDays(7);

        var pending = await dbContext.TaskItems
            .AsNoTracking()
            .CountAsync(t => projectIds.Contains(t.ProjectId) && t.Status != TaskStatus.Completed);

        var completed = await dbContext.TaskItems
            .AsNoTracking()
            .CountAsync(t => projectIds.Contains(t.ProjectId) && t.Status == TaskStatus.Completed);

        var dueSoon = await dbContext.TaskItems
            .AsNoTracking()
            .CountAsync(t =>
                projectIds.Contains(t.ProjectId) &&
                t.DueDate.Date >= today &&
                t.DueDate.Date <= dueSoonDate &&
                t.Status != TaskStatus.Completed);

        var overdue = await dbContext.TaskItems
            .AsNoTracking()
            .CountAsync(t =>
                projectIds.Contains(t.ProjectId) &&
                t.DueDate.Date < today &&
                t.Status != TaskStatus.Completed);

        return new DashboardStatsViewModel
        {
            TotalProjects = projectIds.Count,
            PendingTasks = pending,
            CompletedTasks = completed,
            TasksDueSoon = dueSoon,
            OverdueTasks = overdue
        };
    }

    private IQueryable<Project> AccessibleProjects(string userId)
    {
        return dbContext.Projects.Where(p =>
            p.OwnerId == userId ||
            (p.TeamId != null && p.Team != null && p.Team.Members.Any(m => m.UserId == userId)));
    }
}
