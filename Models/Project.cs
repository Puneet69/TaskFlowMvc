using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TaskItem> Tasks { get; set; } = new();
    public List<ProjectMilestone> Milestones { get; set; } = new();
}
