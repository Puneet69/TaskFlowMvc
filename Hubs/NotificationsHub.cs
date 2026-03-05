using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskFlowMvc.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
}
