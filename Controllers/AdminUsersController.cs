using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;
using TaskFlowMvc.ViewModels;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class AdminUsersController(
    IUserAdministrationService userAdministrationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Index()
    {
        var users = await userAdministrationService.GetUsersAsync();
        var vm = new AdminUsersViewModel
        {
            AvailableRoles = AppRoles.All.ToList()
        };

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            vm.Users.Add((user, roles.ToList()));
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewUser")] AdminCreateUserInput input)
    {
        var result = await userAdministrationService.CreateUserAsync(input.Email, input.Password, input.Role, input.EmailConfirmed);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string role)
    {
        var result = await userAdministrationService.SetUserRoleAsync(userId, role);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(string userId, string reason)
    {
        var result = await userAdministrationService.DisableUserAsync(userId, reason);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(string userId)
    {
        var result = await userAdministrationService.EnableUserAsync(userId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite([Bind(Prefix = "InviteUser")] AdminInviteUserInput input)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var result = await userAdministrationService.InviteUserAsync(input.Email, input.Role, currentUserId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AcceptInvite(string token)
    {
        var currentUserId = userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var result = await userAdministrationService.AcceptInviteAsync(token, currentUserId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), "Dashboard");
    }
}
