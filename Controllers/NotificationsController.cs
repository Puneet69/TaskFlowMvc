using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Services;
using TaskFlowMvc.ViewModels;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class NotificationsController(INotificationService notificationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var vm = new NotificationsViewModel
        {
            Unread = await notificationService.GetUnreadAsync(userId),
            Recent = await notificationService.GetRecentAsync(userId)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var count = await notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        await notificationService.MarkAsReadAsync(userId, id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        await notificationService.MarkAllAsReadAsync(userId);
        return RedirectToAction(nameof(Index));
    }
}
