namespace TaskFlowMvc.ViewModels;

public class DashboardStatsViewModel
{
    public int TotalProjects { get; set; }
    public int TotalTeams { get; set; }
    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int TasksDueSoon { get; set; }
    public int OverdueTasks { get; set; }
}
