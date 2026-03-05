using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class TaskItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Required]
    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    [StringLength(500)]
    public string LabelsCsv { get; set; } = string.Empty;

    public int? ParentTaskId { get; set; }
    public TaskItem? ParentTask { get; set; }
    public List<TaskItem> SubTasks { get; set; } = new();
    public List<TaskDependency> Dependencies { get; set; } = new();
    public List<TaskDependency> DependentTasks { get; set; } = new();

    public bool IsRecurring { get; set; }
    public TaskRecurrenceType RecurrenceType { get; set; } = TaskRecurrenceType.None;
    public int RecurrenceInterval { get; set; } = 1;
    public DateTime? RecursUntilUtc { get; set; }

    public bool IsTemplateBased { get; set; }
    public int? TaskTemplateId { get; set; }
    public TaskTemplate? TaskTemplate { get; set; }

    public List<TaskComment> Comments { get; set; } = new();
    public List<FileAttachment> Attachments { get; set; } = new();
    public List<TimeEntry> TimeEntries { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
