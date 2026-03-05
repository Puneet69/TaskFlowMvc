using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class DashboardController(IProjectService projectService, ITeamService teamService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var stats = await projectService.GetDashboardStatsAsync(userId);
        stats.TotalTeams = await teamService.GetTeamCountForUserAsync(userId);
        return View(stats);
    }
}
