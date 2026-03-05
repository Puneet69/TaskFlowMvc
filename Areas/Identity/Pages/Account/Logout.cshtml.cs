using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LogoutModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ISecurityService securityService,
    ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            var userId = userManager.GetUserId(User);
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var sessionKey = User.FindFirstValue(SecurityClaimTypes.DeviceSessionKey);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await securityService.RecordLoginActivityAsync(userId, email, LoginActivityType.Logout, HttpContext);
                await securityService.MarkCurrentSessionLoggedOutAsync(userId, sessionKey);
            }

            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
        }

        if (returnUrl is not null)
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage();
    }
}
