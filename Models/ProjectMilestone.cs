using System.ComponentModel.DataAnnotations;

namespace TaskFlowMvc.Models;

public class ProjectMilestone
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
