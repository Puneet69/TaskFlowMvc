using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Areas.Identity.Pages.Account;

public class LoginWithOtpModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    ISecurityService securityService,
    ILogger<LoginWithOtpModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(12, MinimumLength = 4)]
        [Display(Name = "OTP code")]
        public string Code { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; } = "/";
    }

    public async Task<IActionResult> OnGetAsync(string? email = null, bool rememberMe = false, string? returnUrl = null)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToPage("./Login");
        }

        Input = new InputModel
        {
            Email = email ?? user.Email ?? string.Empty,
            RememberMe = rememberMe,
            ReturnUrl = returnUrl ?? Url.Content("~/")
        };
        Email = Input.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Email = Input.Email;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToPage("./Login");
        }

        var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, code, Input.RememberMe, rememberClient: false);

        if (result.Succeeded)
        {
            await securityService.RecordLoginActivityAsync(user.Id, user.Email ?? Input.Email, LoginActivityType.TwoFactorSucceeded, HttpContext);
            logger.LogInformation("User logged in with OTP.");
            return LocalRedirect(Input.ReturnUrl ?? Url.Content("~/"));
        }

        if (result.IsLockedOut)
        {
            await securityService.RecordLoginActivityAsync(user.Id, user.Email ?? Input.Email, LoginActivityType.TwoFactorFailed, HttpContext, "Locked out.");
            return RedirectToPage("./Lockout");
        }

        await securityService.RecordLoginActivityAsync(user.Id, user.Email ?? Input.Email, LoginActivityType.TwoFactorFailed, HttpContext, "Invalid OTP.");
        ModelState.AddModelError(string.Empty, "Invalid OTP code.");
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToPage("./Login");
        }

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        await emailSender.SendEmailAsync(user.Email!, "Your TaskFlow OTP", $"Your one-time verification code is <strong>{token}</strong>.");
        await securityService.RecordLoginActivityAsync(user.Id, user.Email ?? Input.Email, LoginActivityType.TwoFactorChallengeSent, HttpContext, "OTP resent.");
        TempData["Info"] = "A new OTP has been sent to your email.";
        return RedirectToPage("./LoginWithOtp", new { email = user.Email, rememberMe = Input.RememberMe, returnUrl = Input.ReturnUrl });
    }
}
