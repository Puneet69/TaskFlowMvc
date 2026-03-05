using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class TeamInvitation
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string InvitedById { get; set; } = string.Empty;

    public ApplicationUser? InvitedBy { get; set; }
    public TeamMemberRole InviteRole { get; set; } = TeamMemberRole.TeamMember;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsAccepted { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
