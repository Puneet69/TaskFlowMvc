using System.ComponentModel.DataAnnotations;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.ViewModels;

public class TeamInviteInputModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public TeamMemberRole Role { get; set; } = TeamMemberRole.TeamMember;
}
