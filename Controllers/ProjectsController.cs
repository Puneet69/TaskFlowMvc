using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlowMvc.Models;
using TaskFlowMvc.Services;
using TaskFlowMvc.ViewModels;
using TaskStatus = TaskFlowMvc.Models.TaskStatus;

namespace TaskFlowMvc.Controllers;

[Authorize]
public class ProjectsController(
    IProjectService projectService,
    ITaskService taskService,
    ITeamService teamService,
    ITimeTrackingService timeTrackingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projects = await projectService.GetProjectsAsync(userId);
        ViewBag.Teams = await teamService.GetTeamsForUserAsync(userId);
        return View(projects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var projects = await projectService.GetProjectsAsync(userId);
            ViewBag.Teams = await teamService.GetTeamsForUserAsync(userId);
            return View("Index", projects);
        }

        await projectService.CreateAsync(model, userId);
        TempData["Success"] = "Project created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var project = await projectService.GetProjectAsync(id, userId);
        if (project is null)
        {
            return NotFound();
        }

        ViewBag.Teams = await teamService.GetTeamsForUserAsync(userId);
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Project model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Teams = await teamService.GetTeamsForUserAsync(userId);
            return View(model);
        }

        var updated = await projectService.UpdateAsync(model, userId);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "Project updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        await projectService.DeleteAsync(id, userId);
        TempData["Success"] = "Project deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await projectService.ArchiveAsync(id, userId);
        TempData[ok ? "Success" : "Error"] = ok ? "Project archived." : "Unable to archive project.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeam(int id, int? teamId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await projectService.AssignTeamAsync(id, teamId, userId);
        TempData[ok ? "Success" : "Error"] = ok ? "Project team updated." : "Unable to assign team.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    public async Task<IActionResult> Details(int id, TaskStatus? statusFilter, TaskPriority? priorityFilter, string? assigneeFilter, string? labelFilter)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var project = await projectService.GetProjectAsync(id, userId);
        if (project is null)
        {
            return NotFound();
        }

        var tasks = await taskService.GetByProjectAsync(id, userId, statusFilter, priorityFilter, assigneeFilter, labelFilter);
        var taskIds = tasks.Select(t => t.Id).ToList();
        var members = project.TeamId is int teamId
            ? (await teamService.GetTeamDetailsAsync(teamId, userId))?.Members ?? new List<TeamMember>()
            : new List<TeamMember>();

        var vm = new ProjectDetailsViewModel
        {
            Project = project,
            Tasks = tasks,
            Milestones = project.Milestones.OrderBy(m => m.DueDate).ToList(),
            TeamMembers = members,
            StatusFilter = statusFilter,
            PriorityFilter = priorityFilter,
            AssigneeFilter = assigneeFilter,
            LabelFilter = labelFilter,
            TaskMinutesSpent = await timeTrackingService.GetTotalMinutesByTaskAsync(taskIds, userId),
            ActiveTimerTaskIds = await timeTrackingService.GetActiveTaskIdsAsync(taskIds, userId),
            NewTask = new TaskItem { ProjectId = id, DueDate = DateTime.Today, Priority = TaskPriority.Medium, Status = TaskStatus.Todo },
            NewMilestone = new ProjectMilestone { ProjectId = id, DueDate = DateTime.Today.AddDays(7) }
        };

        return View(vm);
    }

    public async Task<IActionResult> Timeline(int id)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var project = await projectService.GetProjectAsync(id, userId);
        if (project is null)
        {
            return NotFound();
        }

        var timeline = await projectService.GetProjectTimelineAsync(id, userId);
        ViewBag.Project = project;
        return View(timeline);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMilestone(ProjectMilestone model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid milestone input.";
            return RedirectToAction(nameof(Details), new { id = model.ProjectId });
        }

        var created = await projectService.AddMilestoneAsync(model.ProjectId, model, userId);
        TempData[created is null ? "Error" : "Success"] = created is null ? "Unable to add milestone." : "Milestone added.";
        return RedirectToAction(nameof(Details), new { id = model.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteMilestone(int milestoneId, int projectId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await projectService.CompleteMilestoneAsync(milestoneId, userId);
        TempData[ok ? "Success" : "Error"] = ok ? "Milestone marked completed." : "Unable to complete milestone.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(TaskItem model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id = model.ProjectId });
        }

        await taskService.CreateAsync(model, userId);
        TempData["Success"] = "Task created.";
        return RedirectToAction(nameof(Details), new { id = model.ProjectId });
    }

    public async Task<IActionResult> EditTask(int id, int projectId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var task = await taskService.GetByIdAsync(id, userId);
        if (task is null || task.ProjectId != projectId)
        {
            return NotFound();
        }

        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTask(TaskItem model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await taskService.UpdateAsync(model, userId);
        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "Task updated.";
        return RedirectToAction(nameof(Details), new { id = model.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickEditTask(int taskId, int projectId, string title, TaskPriority priority, string? labelsCsv)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var existing = await taskService.GetByIdAsync(taskId, userId);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Title = title;
        existing.Priority = priority;
        existing.LabelsCsv = labelsCsv ?? string.Empty;
        await taskService.UpdateAsync(existing, userId);
        TempData["Success"] = "Task quick-updated.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(int taskId, int projectId, string? assignedToId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        await taskService.AssignTaskAsync(taskId, assignedToId, userId);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(int taskId, int projectId, TaskStatus status)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        await taskService.UpdateStatusAsync(taskId, status, userId);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateStatus(int projectId, List<int> taskIds, TaskStatus status)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var updated = await taskService.BulkUpdateStatusAsync(taskIds, status, userId);
        TempData["Success"] = $"{updated} tasks updated.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAssign(int projectId, List<int> taskIds, string? assignedToId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var updated = await taskService.BulkAssignAsync(taskIds, assignedToId, userId);
        TempData["Success"] = $"{updated} tasks reassigned.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSubTask(int parentTaskId, int projectId, string title, string description, DateTime dueDate, TaskPriority priority)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var subTask = new TaskItem
        {
            Title = title,
            Description = description,
            DueDate = dueDate,
            Priority = priority,
            Status = TaskStatus.Todo
        };
        await taskService.AddSubTaskAsync(parentTaskId, subTask, userId);
        TempData["Success"] = "Subtask added.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartTimer(int taskId, int projectId, string? note)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await timeTrackingService.StartTimerAsync(taskId, userId, note);
        TempData[ok ? "Success" : "Error"] = ok ? "Timer started." : "Unable to start timer.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopTimer(int taskId, int projectId, string? note)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await timeTrackingService.StopTimerAsync(taskId, userId, note);
        TempData[ok ? "Success" : "Error"] = ok ? "Timer stopped." : "No active timer found.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogTime(int taskId, int projectId, int minutes, string? note, DateTime? startAtUtc)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var entry = await timeTrackingService.LogManualAsync(taskId, userId, minutes, note, startAtUtc);
        TempData[entry is null ? "Error" : "Success"] = entry is null ? "Unable to log time." : "Time logged.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDependency(int taskId, int projectId, int dependsOnTaskId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await taskService.AddDependencyAsync(taskId, dependsOnTaskId, userId);
        TempData[ok ? "Success" : "Error"] = ok ? "Dependency added." : "Unable to add dependency.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTaskTemplate(int taskId, int projectId, string templateName)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var template = await taskService.SaveTemplateAsync(taskId, templateName, userId);
        TempData[template is null ? "Error" : "Success"] = template is null ? "Unable to save template." : "Template saved.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTaskFromTemplate(int projectId, int templateId, DateTime dueDate)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var task = await taskService.CreateFromTemplateAsync(projectId, templateId, dueDate, userId);
        TempData[task is null ? "Error" : "Success"] = task is null ? "Unable to create from template." : "Task created from template.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int taskId, int projectId, string content, List<IFormFile>? files)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var comment = await taskService.AddCommentAsync(taskId, content, userId, files);
        TempData[comment is null ? "Error" : "Success"] = comment is null ? "Unable to add comment." : "Comment added.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(int commentId, int projectId, string content)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await taskService.EditCommentAsync(commentId, content, userId);
        TempData[ok ? "Success" : "Error"] = ok ? "Comment updated." : "Unable to edit comment.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId, int projectId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var ok = await taskService.DeleteCommentAsync(commentId, userId);
        TempData[ok ? "Success" : "Error"] = ok ? "Comment deleted." : "Unable to delete comment.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReaction(int commentId, int projectId, string emoji)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        await taskService.ToggleReactionAsync(commentId, emoji, userId);
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int taskId, int projectId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        await taskService.DeleteAsync(taskId, userId);
        TempData["Success"] = "Task deleted.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    private string? GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}
