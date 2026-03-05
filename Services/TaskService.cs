using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Data;
using TaskFlowMvc.Models;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Services;

public class TaskService(
    ApplicationDbContext dbContext,
    INotificationService notificationService,
    IFileStorageService fileStorageService) : ITaskService
{
    public async Task<List<TaskItem>> GetByProjectAsync(int projectId, string userId, TaskStatus? status = null, TaskPriority? priority = null, string? assigneeId = null, string? label = null)
    {
        var hasAccess = await HasProjectAccessAsync(projectId, userId);
        if (!hasAccess)
        {
            return new List<TaskItem>();
        }

        var query = dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.AssignedTo)
            .Include(t => t.SubTasks)
            .Include(t => t.Dependencies)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.AuthorUser)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Reactions)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Attachments)
            .Include(t => t.Attachments)
            .Where(t => t.ProjectId == projectId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(assigneeId))
        {
            query = query.Where(t => t.AssignedToId == assigneeId);
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            var normalized = NormalizeLabel(label);
            query = query.Where(t => t.LabelsCsv.Contains(normalized));
        }

        return await query
            .OrderBy(t => t.Status)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int taskId, string userId)
    {
        var task = await dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.AuthorUser)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Reactions)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Attachments)
            .Include(t => t.Attachments)
            .Include(t => t.SubTasks)
            .Include(t => t.Dependencies)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
        {
            return null;
        }

        return await HasProjectAccessAsync(task.ProjectId, userId) ? task : null;
    }

    public async Task<TaskItem> CreateAsync(TaskItem taskItem, string userId)
    {
        var hasAccess = await HasProjectAccessAsync(taskItem.ProjectId, userId);
        if (!hasAccess)
        {
            throw new InvalidOperationException("Project not found.");
        }

        taskItem.CreatedAt = DateTime.UtcNow;
        taskItem.LabelsCsv = NormalizeLabelsCsv(taskItem.LabelsCsv);
        dbContext.TaskItems.Add(taskItem);
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(taskItem.AssignedToId) && taskItem.AssignedToId != userId)
        {
            await notificationService.NotifyAsync(
                taskItem.AssignedToId,
                NotificationType.TaskAssigned,
                "Task assigned",
                $"You were assigned task: {taskItem.Title}",
                $"/Projects/Details/{taskItem.ProjectId}",
                sendEmail: true);
        }

        return taskItem;
    }

    public async Task<bool> UpdateAsync(TaskItem taskItem, string userId)
    {
        var existing = await dbContext.TaskItems
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskItem.Id);
        if (existing is null || !await HasProjectAccessAsync(existing.ProjectId, userId))
        {
            return false;
        }

        var previousAssignee = existing.AssignedToId;

        existing.Title = taskItem.Title;
        existing.Description = taskItem.Description;
        existing.DueDate = taskItem.DueDate;
        existing.Priority = taskItem.Priority;
        existing.Status = taskItem.Status;
        existing.AssignedToId = taskItem.AssignedToId;
        existing.LabelsCsv = NormalizeLabelsCsv(taskItem.LabelsCsv);
        existing.IsRecurring = taskItem.IsRecurring;
        existing.RecurrenceType = taskItem.IsRecurring ? taskItem.RecurrenceType : TaskRecurrenceType.None;
        existing.RecurrenceInterval = Math.Max(1, taskItem.RecurrenceInterval);
        existing.RecursUntilUtc = taskItem.RecursUntilUtc;
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(existing.AssignedToId) && existing.AssignedToId != userId)
        {
            await notificationService.NotifyAsync(
                existing.AssignedToId,
                NotificationType.TaskUpdated,
                "Task updated",
                $"Task '{existing.Title}' was updated.",
                $"/Projects/Details/{existing.ProjectId}");
        }

        if (!string.Equals(previousAssignee, existing.AssignedToId, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(existing.AssignedToId) &&
            existing.AssignedToId != userId)
        {
            await notificationService.NotifyAsync(
                existing.AssignedToId,
                NotificationType.TaskAssigned,
                "Task reassigned",
                $"Task '{existing.Title}' was assigned to you.",
                $"/Projects/Details/{existing.ProjectId}",
                sendEmail: true);
        }

        return true;
    }

    public async Task<bool> UpdateStatusAsync(int taskId, TaskStatus status, string userId)
    {
        var existing = await dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
        if (existing is null || !await HasProjectAccessAsync(existing.ProjectId, userId))
        {
            return false;
        }

        existing.Status = status;
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(existing.AssignedToId) && existing.AssignedToId != userId)
        {
            await notificationService.NotifyAsync(
                existing.AssignedToId,
                NotificationType.TaskUpdated,
                "Task status changed",
                $"Task '{existing.Title}' moved to {status}.",
                $"/Projects/Details/{existing.ProjectId}");
        }

        return true;
    }

    public async Task<bool> AssignTaskAsync(int taskId, string? assignedToId, string userId)
    {
        var existing = await dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
        if (existing is null || !await HasProjectAccessAsync(existing.ProjectId, userId))
        {
            return false;
        }

        existing.AssignedToId = string.IsNullOrWhiteSpace(assignedToId) ? null : assignedToId.Trim();
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(existing.AssignedToId) && existing.AssignedToId != userId)
        {
            await notificationService.NotifyAsync(
                existing.AssignedToId,
                NotificationType.TaskAssigned,
                "Task assigned",
                $"You were assigned task: {existing.Title}",
                $"/Projects/Details/{existing.ProjectId}",
                sendEmail: true);
        }

        return true;
    }

    public async Task<bool> AddDependencyAsync(int taskId, int dependsOnTaskId, string userId)
    {
        if (taskId == dependsOnTaskId)
        {
            return false;
        }

        var task = await dbContext.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        var dependency = await dbContext.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == dependsOnTaskId);
        if (task is null || dependency is null || task.ProjectId != dependency.ProjectId)
        {
            return false;
        }

        if (!await HasProjectAccessAsync(task.ProjectId, userId))
        {
            return false;
        }

        var exists = await dbContext.TaskDependencies.AnyAsync(d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId);
        if (exists)
        {
            return true;
        }

        dbContext.TaskDependencies.Add(new TaskDependency
        {
            TaskId = taskId,
            DependsOnTaskId = dependsOnTaskId
        });
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveDependencyAsync(int taskId, int dependsOnTaskId, string userId)
    {
        var task = await dbContext.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || !await HasProjectAccessAsync(task.ProjectId, userId))
        {
            return false;
        }

        var row = await dbContext.TaskDependencies.FirstOrDefaultAsync(d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId);
        if (row is null)
        {
            return false;
        }

        dbContext.TaskDependencies.Remove(row);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<TaskItem?> AddSubTaskAsync(int parentTaskId, TaskItem taskItem, string userId)
    {
        var parent = await dbContext.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == parentTaskId);
        if (parent is null || !await HasProjectAccessAsync(parent.ProjectId, userId))
        {
            return null;
        }

        taskItem.ProjectId = parent.ProjectId;
        taskItem.ParentTaskId = parentTaskId;
        taskItem.CreatedAt = DateTime.UtcNow;
        taskItem.LabelsCsv = NormalizeLabelsCsv(taskItem.LabelsCsv);
        dbContext.TaskItems.Add(taskItem);
        await dbContext.SaveChangesAsync();
        return taskItem;
    }

    public async Task<TaskTemplate?> SaveTemplateAsync(int taskId, string templateName, string userId)
    {
        var task = await dbContext.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || !await HasProjectAccessAsync(task.ProjectId, userId))
        {
            return null;
        }

        var template = new TaskTemplate
        {
            Name = string.IsNullOrWhiteSpace(templateName) ? $"Template {DateTime.UtcNow:yyyyMMddHHmm}" : templateName.Trim(),
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            LabelsCsv = task.LabelsCsv,
            OwnerId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.TaskTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        return template;
    }

    public async Task<TaskItem?> CreateFromTemplateAsync(int projectId, int templateId, DateTime dueDate, string userId)
    {
        if (!await HasProjectAccessAsync(projectId, userId))
        {
            return null;
        }

        var template = await dbContext.TaskTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.OwnerId == userId);
        if (template is null)
        {
            return null;
        }

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = template.Title,
            Description = template.Description,
            Priority = template.Priority,
            DueDate = dueDate.Date,
            Status = TaskStatus.Todo,
            LabelsCsv = template.LabelsCsv,
            IsTemplateBased = true,
            TaskTemplateId = template.Id,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.TaskItems.Add(task);
        await dbContext.SaveChangesAsync();
        return task;
    }

    public async Task<int> BulkUpdateStatusAsync(List<int> taskIds, TaskStatus status, string userId)
    {
        var ids = taskIds.Distinct().ToList();
        var tasks = await dbContext.TaskItems
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();

        var updated = 0;
        foreach (var task in tasks)
        {
            if (!await HasProjectAccessAsync(task.ProjectId, userId))
            {
                continue;
            }

            task.Status = status;
            updated++;
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        return updated;
    }

    public async Task<int> BulkAssignAsync(List<int> taskIds, string? assignedToId, string userId)
    {
        var ids = taskIds.Distinct().ToList();
        var tasks = await dbContext.TaskItems
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();

        var updated = 0;
        foreach (var task in tasks)
        {
            if (!await HasProjectAccessAsync(task.ProjectId, userId))
            {
                continue;
            }

            task.AssignedToId = string.IsNullOrWhiteSpace(assignedToId) ? null : assignedToId.Trim();
            updated++;
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(int taskId, string userId)
    {
        var existing = await dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
        if (existing is null || !await HasProjectAccessAsync(existing.ProjectId, userId))
        {
            return false;
        }

        dbContext.TaskItems.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<TaskComment?> AddCommentAsync(int taskId, string content, string userId, IEnumerable<IFormFile>? files = null)
    {
        var safeContent = content ?? string.Empty;
        var task = await dbContext.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null || !await HasProjectAccessAsync(task.ProjectId, userId))
        {
            return null;
        }

        var mentionedUserIds = await ResolveMentionedUserIdsAsync(safeContent, userId);

        var comment = new TaskComment
        {
            TaskItemId = taskId,
            AuthorUserId = userId,
            Content = safeContent.Trim(),
            MentionedUserIdsCsv = ToMentionCsv(mentionedUserIds),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.TaskComments.Add(comment);
        await dbContext.SaveChangesAsync();

        if (files is not null)
        {
            foreach (var file in files.Where(f => f.Length > 0))
            {
                var saved = await fileStorageService.SaveAsync(file, "comments");
                var nextVersion = await NextAttachmentVersionAsync(saved.FileName, taskId, comment.Id);
                dbContext.FileAttachments.Add(new FileAttachment
                {
                    TaskCommentId = comment.Id,
                    TaskItemId = taskId,
                    UploadedByUserId = userId,
                    FileName = saved.FileName,
                    StoredPath = saved.StoredPath,
                    ContentType = saved.ContentType,
                    SizeBytes = saved.SizeBytes,
                    Version = nextVersion,
                    UploadedAtUtc = DateTime.UtcNow
                });
            }
            await dbContext.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(task.AssignedToId) && task.AssignedToId != userId)
        {
            await notificationService.NotifyAsync(
                task.AssignedToId,
                NotificationType.CommentAdded,
                "New task comment",
                $"A new comment was added to '{task.Title}'.",
                $"/Projects/Details/{task.ProjectId}");
        }

        foreach (var mentionedUserId in mentionedUserIds)
        {
            await notificationService.NotifyAsync(
                mentionedUserId,
                NotificationType.MentionedInComment,
                "You were mentioned",
                $"You were mentioned in a comment on task '{task.Title}'.",
                $"/Projects/Details/{task.ProjectId}");
        }

        return await dbContext.TaskComments
            .AsNoTracking()
            .Include(c => c.AuthorUser)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == comment.Id);
    }

    public async Task<bool> EditCommentAsync(int commentId, string content, string userId)
    {
        var safeContent = content ?? string.Empty;
        var comment = await dbContext.TaskComments
            .Include(c => c.TaskItem)
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        if (comment is null || comment.AuthorUserId != userId)
        {
            return false;
        }

        comment.Content = safeContent.Trim();
        comment.UpdatedAtUtc = DateTime.UtcNow;
        comment.MentionedUserIdsCsv = ToMentionCsv(await ResolveMentionedUserIdsAsync(safeContent, userId));
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, string userId)
    {
        var comment = await dbContext.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        if (comment is null || comment.AuthorUserId != userId)
        {
            return false;
        }

        comment.IsDeleted = true;
        comment.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleReactionAsync(int commentId, string emoji, string userId)
    {
        if (string.IsNullOrWhiteSpace(emoji))
        {
            return false;
        }

        var comment = await dbContext.TaskComments
            .Include(c => c.TaskItem)
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        if (comment is null || comment.TaskItem is null || !await HasProjectAccessAsync(comment.TaskItem.ProjectId, userId))
        {
            return false;
        }

        var existing = await dbContext.CommentReactions
            .FirstOrDefaultAsync(r => r.TaskCommentId == commentId && r.UserId == userId && r.Emoji == emoji);
        if (existing is null)
        {
            dbContext.CommentReactions.Add(new CommentReaction
            {
                TaskCommentId = commentId,
                UserId = userId,
                Emoji = emoji,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.CommentReactions.Remove(existing);
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<FileAttachment?> GetAttachmentAsync(int attachmentId, string userId)
    {
        var attachment = await dbContext.FileAttachments
            .AsNoTracking()
            .Include(a => a.TaskItem)
            .Include(a => a.TaskComment!)
                .ThenInclude(c => c.TaskItem)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (attachment is null)
        {
            return null;
        }

        var projectId = attachment.TaskItem?.ProjectId ?? attachment.TaskComment?.TaskItem?.ProjectId;
        if (!projectId.HasValue || !await HasProjectAccessAsync(projectId.Value, userId))
        {
            return null;
        }

        return attachment;
    }

    public async Task<List<TaskItem>> GetTimelineTasksAsync(string userId)
    {
        var projectIds = await GetAccessibleProjectIdsAsync(userId);
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(t => projectIds.Contains(t.ProjectId))
            .Include(t => t.AssignedTo)
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetOverdueTasksAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var projectIds = await GetAccessibleProjectIdsAsync(userId);
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(t => projectIds.Contains(t.ProjectId) && t.DueDate.Date < today && t.Status != TaskStatus.Completed)
            .Include(t => t.AssignedTo)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    private async Task<List<int>> GetAccessibleProjectIdsAsync(string userId)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == userId || (p.Team != null && p.Team.Members.Any(m => m.UserId == userId)))
            .Select(p => p.Id)
            .ToListAsync();
    }

    private async Task<bool> HasProjectAccessAsync(int projectId, string userId)
    {
        return await dbContext.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == projectId && (p.OwnerId == userId || (p.Team != null && p.Team.Members.Any(m => m.UserId == userId))));
    }

    private static string NormalizeLabelsCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return string.Empty;
        }

        var labels = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeLabel)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return string.Join(",", labels);
    }

    private static string NormalizeLabel(string label)
    {
        return (label ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string ToMentionCsv(IEnumerable<string> userIds)
    {
        var csv = string.Join(",", userIds);
        return csv.Length <= 500 ? csv : csv[..500];
    }

    private async Task<List<string>> ResolveMentionedUserIdsAsync(string content, string currentUserId)
    {
        var tokens = ExtractMentionTokens(content)
            .Select(t => t.Trim().Trim(',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\''))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tokens.Count == 0)
        {
            return [];
        }

        var normalizedTokens = tokens.Select(t => t.ToUpperInvariant()).ToList();
        var ids = await dbContext.Users
            .AsNoTracking()
            .Where(u =>
                tokens.Contains(u.Id) ||
                (u.NormalizedUserName != null && normalizedTokens.Contains(u.NormalizedUserName)) ||
                (u.NormalizedEmail != null && normalizedTokens.Contains(u.NormalizedEmail)))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync();

        return ids
            .Where(id => !string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static List<string> ExtractMentionTokens(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var matches = System.Text.RegularExpressions.Regex.Matches(content, @"@([A-Za-z0-9_\-\.@]+)");
        return matches
            .Select(m => m.Groups[1].Value.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    private async Task<int> NextAttachmentVersionAsync(string fileName, int taskId, int commentId)
    {
        var maxVersion = await dbContext.FileAttachments
            .AsNoTracking()
            .Where(a => a.TaskItemId == taskId && a.TaskCommentId == commentId && a.FileName == fileName)
            .Select(a => (int?)a.Version)
            .MaxAsync();
        return (maxVersion ?? 0) + 1;
    }
}
