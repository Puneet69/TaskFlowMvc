using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Models;

public class Team
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TeamMember> Members { get; set; } = new();
    public List<TeamInvitation> Invitations { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public List<TeamActivityLog> ActivityLogs { get; set; } = new();
}
