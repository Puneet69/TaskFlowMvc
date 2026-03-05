using TaskFlowMvc.ViewModels;

namespace TaskFlowMvc.Services;

public interface IReportService
{
    Task<ReportsViewModel> GetProjectReportAsync(string userId, DateTime fromDateUtc, DateTime toDateUtc);
    Task<string> BuildProjectReportCsvAsync(string userId, DateTime fromDateUtc, DateTime toDateUtc);
}
