using TaskFlowMvc.Data;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.ViewModels;

public class AdminUsersViewModel
{
    public List<(ApplicationUser User, List<string> Roles)> Users { get; set; } = new();
    public AdminCreateUserInput NewUser { get; set; } = new();
    public AdminInviteUserInput InviteUser { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = AppRoles.All.ToList();
}

public class AdminCreateUserInput
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = "TaskFlow!123";
    public string Role { get; set; } = AppRoles.Viewer;
    public bool EmailConfirmed { get; set; } = true;
}

public class AdminInviteUserInput
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Viewer;
}
