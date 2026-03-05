namespace TaskFlowMvc.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string TeamLeader = "TeamLeader";
    public const string TeamMember = "TeamMember";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, TeamLeader, TeamMember, Viewer];
}
