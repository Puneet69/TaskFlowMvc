using TaskFlowMvc.Models;
using TaskFlowMvc.ViewModels;

namespace TaskFlowMvc.Services;

public interface IProjectService
{
    Task<List<Project>> GetProjectsAsync(string userId);
    Task<Project?> GetProjectAsync(int id, string userId);
    Task<List<Project>> GetProjectsForTeamAsync(int teamId, string userId);
    Task<Project> CreateAsync(Project project, string userId);
    Task<bool> UpdateAsync(Project project, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> ArchiveAsync(int id, string userId);
    Task<bool> UnarchiveAsync(int id, string userId);
    Task<bool> AssignTeamAsync(int projectId, int? teamId, string userId);
    Task<ProjectMilestone?> AddMilestoneAsync(int projectId, ProjectMilestone milestone, string userId);
    Task<bool> CompleteMilestoneAsync(int milestoneId, string userId);
    Task<List<(DateTime Date, string Type, string Title, string Description)>> GetProjectTimelineAsync(int projectId, string userId);
    Task<DashboardStatsViewModel> GetDashboardStatsAsync(string userId);
}
