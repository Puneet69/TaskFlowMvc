using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class CommentReaction
{
    public int Id { get; set; }

    public int TaskCommentId { get; set; }
    public TaskComment? TaskComment { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Emoji { get; set; } = "\uD83D\uDC4D";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
