using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using TaskFlowMvc.Data;

namespace TaskFlowMvc.Services;

public class UserVerificationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    LinkGenerator linkGenerator,
    IConfiguration configuration,
    ILogger<UserVerificationService> logger) : IUserVerificationService
{
    private const string VerificationSessionKey = "VerificationEmailSentAtUtc";

    public async Task<bool> SendVerificationEmailIfNeededAsync(System.Security.Claims.ClaimsPrincipal principal, HttpContext httpContext)
    {
        if (!principal.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        if (httpContext.Session.TryGetValue(VerificationSessionKey, out _))
        {
            return false;
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return false;
        }

        try
        {
            if (!IsSmtpConfigured())
            {
                logger.LogWarning("SMTP is not configured. Verification email was not sent for user {UserId}", user.Id);
                return false;
            }

            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = linkGenerator.GetUriByPage(httpContext, "/Account/ConfirmEmail", values: new
            {
                area = "Identity",
                userId = user.Id,
                code = encodedCode,
                returnUrl = "/"
            });

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                logger.LogWarning("Could not build confirmation URL for user {UserId}", user.Id);
                return false;
            }

            await emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email - TaskFlow",
                $"Please confirm your account by <a href='{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            httpContext.Session.SetString(VerificationSessionKey, DateTime.UtcNow.ToString("O"));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed sending verification email for user {UserId}", user.Id);
            return false;
        }
    }

    private bool IsSmtpConfigured()
    {
        var host = configuration["Email:Smtp:Host"];
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var fromEmail = configuration["Email:Smtp:FromEmail"];

        return !string.IsNullOrWhiteSpace(host) &&
               !string.IsNullOrWhiteSpace(username) &&
               !string.IsNullOrWhiteSpace(password) &&
               !string.IsNullOrWhiteSpace(fromEmail);
    }
}
