using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class AttachmentsController(ITaskService taskService, IFileStorageService fileStorageService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var attachment = await taskService.GetAttachmentAsync(id, userId);
        if (attachment is null)
        {
            return NotFound();
        }

        try
        {
            var file = await fileStorageService.OpenReadAsync(attachment.StoredPath);
            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? file.ContentType : attachment.ContentType;
            return File(file.Stream, contentType, enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var attachment = await taskService.GetAttachmentAsync(id, userId);
        if (attachment is null)
        {
            return NotFound();
        }

        try
        {
            var file = await fileStorageService.OpenReadAsync(attachment.StoredPath);
            var contentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? file.ContentType : attachment.ContentType;
            var downloadName = string.IsNullOrWhiteSpace(attachment.FileName) ? file.FileName : attachment.FileName;
            return File(file.Stream, contentType, downloadName, enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
