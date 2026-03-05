using TaskFlowMvc.Models;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.ViewModels;

public class ProjectDetailsViewModel
{
    public Project Project { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public TaskItem NewTask { get; set; } = new();
    public List<ProjectMilestone> Milestones { get; set; } = new();
    public ProjectMilestone NewMilestone { get; set; } = new();
    public List<TeamMember> TeamMembers { get; set; } = new();
    public List<TaskTemplate> Templates { get; set; } = new();
    public TaskStatus? StatusFilter { get; set; }
    public TaskPriority? PriorityFilter { get; set; }
    public string? AssigneeFilter { get; set; }
    public string? LabelFilter { get; set; }
    public Dictionary<int, int> TaskMinutesSpent { get; set; } = new();
    public HashSet<int> ActiveTimerTaskIds { get; set; } = new();
}
