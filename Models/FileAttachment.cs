using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class FileAttachment
{
    public int Id { get; set; }

    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public int? TaskCommentId { get; set; }
    public TaskComment? TaskComment { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;
    public ApplicationUser? UploadedByUser { get; set; }

    [Required]
    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(1024)]
    public string StoredPath { get; set; } = string.Empty;

    [StringLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }
    public int Version { get; set; } = 1;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
