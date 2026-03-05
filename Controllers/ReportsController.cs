using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Services;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class ReportsController(IReportService reportService) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDateUtc, DateTime? toDateUtc)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var from = fromDateUtc?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = toDateUtc?.Date ?? DateTime.UtcNow.Date;
        var vm = await reportService.GetProjectReportAsync(userId, from, to);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateTime? fromDateUtc, DateTime? toDateUtc)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var from = fromDateUtc?.Date ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = toDateUtc?.Date ?? DateTime.UtcNow.Date;
        var csv = await reportService.BuildProjectReportCsvAsync(userId, from, to);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var name = $"taskflow-report-{from:yyyyMMdd}-{to:yyyyMMdd}.csv";
        return File(bytes, "text/csv", name);
    }
}
