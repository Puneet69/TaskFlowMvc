using TaskFlowMvc.Models;

namespace TaskFlowMvc.ViewModels;

public class TeamIndexViewModel
{
    public TeamCreateInputModel NewTeam { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
}
