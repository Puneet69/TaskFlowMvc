using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class TimelineController(ITaskService taskService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var tasks = await taskService.GetTimelineTasksAsync(userId);
        var grouped = tasks.GroupBy(t => t.DueDate.Date).OrderBy(g => g.Key).ToList();
        return View(grouped);
    }
}
