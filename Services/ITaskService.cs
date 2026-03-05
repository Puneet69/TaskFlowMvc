using Microsoft.AspNetCore.Http;
using TaskFlowMvc.Models;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Services;

public interface ITaskService
{
    Task<List<TaskItem>> GetByProjectAsync(int projectId, string userId, TaskStatus? status = null, TaskPriority? priority = null, string? assigneeId = null, string? label = null);
    Task<TaskItem?> GetByIdAsync(int taskId, string userId);
    Task<TaskItem> CreateAsync(TaskItem taskItem, string userId);
    Task<bool> UpdateAsync(TaskItem taskItem, string userId);
    Task<bool> UpdateStatusAsync(int taskId, TaskStatus status, string userId);
    Task<bool> AssignTaskAsync(int taskId, string? assignedToId, string userId);
    Task<bool> AddDependencyAsync(int taskId, int dependsOnTaskId, string userId);
    Task<bool> RemoveDependencyAsync(int taskId, int dependsOnTaskId, string userId);
    Task<TaskItem?> AddSubTaskAsync(int parentTaskId, TaskItem taskItem, string userId);
    Task<TaskTemplate?> SaveTemplateAsync(int taskId, string templateName, string userId);
    Task<TaskItem?> CreateFromTemplateAsync(int projectId, int templateId, DateTime dueDate, string userId);
    Task<int> BulkUpdateStatusAsync(List<int> taskIds, TaskStatus status, string userId);
    Task<int> BulkAssignAsync(List<int> taskIds, string? assignedToId, string userId);
    Task<bool> DeleteAsync(int taskId, string userId);
    Task<TaskComment?> AddCommentAsync(int taskId, string content, string userId, IEnumerable<IFormFile>? files = null);
    Task<bool> EditCommentAsync(int commentId, string content, string userId);
    Task<bool> DeleteCommentAsync(int commentId, string userId);
    Task<bool> ToggleReactionAsync(int commentId, string emoji, string userId);
    Task<FileAttachment?> GetAttachmentAsync(int attachmentId, string userId);
    Task<List<TaskItem>> GetTimelineTasksAsync(string userId);
    Task<List<TaskItem>> GetOverdueTasksAsync(string userId);
}
