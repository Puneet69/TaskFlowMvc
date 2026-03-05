namespace TaskFlowMvc.Models;

public class TaskDependency
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public int DependsOnTaskId { get; set; }
    public TaskItem? DependsOnTask { get; set; }
}
