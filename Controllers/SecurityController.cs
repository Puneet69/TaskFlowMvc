using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;
using TaskFlowMvc.ViewModels;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class SecurityController(ISecurityService securityService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var vm = new SecurityDashboardViewModel
        {
            Sessions = await securityService.GetActiveDeviceSessionsAsync(userId),
            LoginActivities = await securityService.GetLoginActivityAsync(userId, 100),
            CurrentSessionKey = User.FindFirstValue(SecurityClaimTypes.DeviceSessionKey) ?? string.Empty
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutOtherDevices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionKey = User.FindFirstValue(SecurityClaimTypes.DeviceSessionKey);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionKey))
        {
            return Challenge();
        }

        await securityService.RevokeOtherSessionsAsync(userId, sessionKey);
        TempData["Success"] = "Other devices were logged out.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeDevice(string sessionKey)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionKey))
        {
            return Challenge();
        }

        var current = User.FindFirstValue(SecurityClaimTypes.DeviceSessionKey);
        if (string.Equals(current, sessionKey, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Use normal logout for current device.";
            return RedirectToAction(nameof(Index));
        }

        await securityService.RevokeSessionAsync(userId, sessionKey);
        TempData["Success"] = "Device session revoked.";
        return RedirectToAction(nameof(Index));
    }
}
