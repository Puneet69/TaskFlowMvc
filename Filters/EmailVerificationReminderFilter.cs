using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Filters;

public class EmailVerificationReminderFilter(
    IUserVerificationService verificationService,
    ITempDataDictionaryFactory tempDataFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            await next();
            return;
        }

        if (context.HttpContext.Request.Path.StartsWithSegments("/Identity/Account/ConfirmEmail", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var sent = await verificationService.SendVerificationEmailIfNeededAsync(context.HttpContext.User, context.HttpContext);
        if (sent)
        {
            var tempData = tempDataFactory.GetTempData(context.HttpContext);
            tempData["Info"] = "A verification email has been sent to your inbox.";
        }

        await next();
    }
}
