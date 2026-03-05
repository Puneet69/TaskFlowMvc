using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class UserInvitation
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Role { get; set; } = AppRoles.Viewer;

    [Required]
    [StringLength(128)]
    public string Token { get; set; } = string.Empty;

    public string InvitedById { get; set; } = string.Empty;
    public ApplicationUser? InvitedBy { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsAccepted { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
}
