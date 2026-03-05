using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class TaskComment
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    [Required]
    public string AuthorUserId { get; set; } = string.Empty;
    public ApplicationUser? AuthorUser { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(500)]
    public string MentionedUserIdsCsv { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    public List<CommentReaction> Reactions { get; set; } = new();
    public List<FileAttachment> Attachments { get; set; } = new();
}
