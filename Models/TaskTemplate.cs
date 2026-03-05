using System.ComponentModel.DataAnnotations;

namespace TaskFlowMvc.Models;

public class TaskTemplate
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [StringLength(500)]
    public string LabelsCsv { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
