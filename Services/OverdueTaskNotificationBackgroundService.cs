using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Services;

public class OverdueTaskNotificationBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<OverdueTaskNotificationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Overdue task notification pass failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var today = DateTime.UtcNow.Date;

        var overdue = await dbContext.TaskItems
            .AsNoTracking()
            .Where(t =>
                t.AssignedToId != null &&
                t.DueDate.Date < today &&
                t.Status != TaskStatus.Completed)
            .Select(t => new { t.Id, t.Title, t.ProjectId, t.AssignedToId, t.DueDate })
            .ToListAsync(cancellationToken);

        foreach (var task in overdue)
        {
            if (task.AssignedToId is null)
            {
                continue;
            }

            var link = $"/Projects/Details/{task.ProjectId}";
            var alreadySentToday = await dbContext.NotificationItems
                .AsNoTracking()
                .AnyAsync(n =>
                    n.UserId == task.AssignedToId &&
                    n.Type == NotificationType.TaskOverdue &&
                    n.LinkUrl == link &&
                    n.CreatedAtUtc.Date == today,
                    cancellationToken);
            if (alreadySentToday)
            {
                continue;
            }

            await notificationService.NotifyAsync(
                task.AssignedToId,
                NotificationType.TaskOverdue,
                "Task overdue",
                $"Task '{task.Title}' is overdue since {task.DueDate:yyyy-MM-dd}.",
                link,
                sendEmail: true);
        }
    }
}
