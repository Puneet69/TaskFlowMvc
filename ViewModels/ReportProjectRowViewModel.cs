namespace TaskFlowMvc.ViewModels;

public class ReportProjectRowViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int TotalTrackedMinutes { get; set; }
}
