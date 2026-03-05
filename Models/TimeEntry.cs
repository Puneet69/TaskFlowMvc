using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class TimeEntry
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }

    public int MinutesSpent { get; set; }

    [StringLength(500)]
    public string Note { get; set; } = string.Empty;

    public bool IsManualEntry { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
