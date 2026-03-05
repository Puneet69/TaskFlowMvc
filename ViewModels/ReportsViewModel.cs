namespace TaskFlowMvc.ViewModels;

public class ReportsViewModel
{
    public DateTime FromDateUtc { get; set; }
    public DateTime ToDateUtc { get; set; }
    public List<ReportProjectRowViewModel> Projects { get; set; } = new();

    public int TotalProjects => Projects.Count;
    public int TotalTasks => Projects.Sum(p => p.TotalTasks);
    public int CompletedTasks => Projects.Sum(p => p.CompletedTasks);
    public int PendingTasks => Projects.Sum(p => p.PendingTasks);
    public int OverdueTasks => Projects.Sum(p => p.OverdueTasks);
    public int TotalTrackedMinutes => Projects.Sum(p => p.TotalTrackedMinutes);
}
