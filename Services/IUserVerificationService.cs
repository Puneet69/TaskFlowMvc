using System.Security.Claims;

namespace TaskFlowMvc.Services;

public interface IUserVerificationService
{
    Task<bool> SendVerificationEmailIfNeededAsync(ClaimsPrincipal principal, HttpContext httpContext);
}
