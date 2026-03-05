using TaskFlowMvc.Models;

namespace TaskFlowMvc.ViewModels;

public class SecurityDashboardViewModel
{
    public List<DeviceSession> Sessions { get; set; } = new();
    public List<LoginActivity> LoginActivities { get; set; } = new();
    public string CurrentSessionKey { get; set; } = string.Empty;
}
