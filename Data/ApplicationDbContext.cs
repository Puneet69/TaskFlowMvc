using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlowMvc.Models;

namespace TaskFlowMvc.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<CommentReaction> CommentReactions => Set<CommentReaction>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();
    public DbSet<TeamActivityLog> TeamActivityLogs => Set<TeamActivityLog>();

    public DbSet<LoginActivity> LoginActivities => Set<LoginActivity>();
    public DbSet<DeviceSession> DeviceSessions => Set<DeviceSession>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<NotificationItem> NotificationItems => Set<NotificationItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.OwnedProjects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Project>()
            .HasOne(p => p.Team)
            .WithMany(t => t.Projects)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Project>()
            .HasIndex(p => new { p.OwnerId, p.Status, p.IsArchived });

        builder.Entity<TaskItem>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TaskItem>()
            .HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TaskItem>()
            .HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaskItem>()
            .HasOne(t => t.TaskTemplate)
            .WithMany()
            .HasForeignKey(t => t.TaskTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TaskItem>()
            .HasIndex(t => new { t.ProjectId, t.Status, t.DueDate });

        builder.Entity<ProjectMilestone>()
            .HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectMilestone>()
            .HasIndex(m => new { m.ProjectId, m.DueDate });

        builder.Entity<TaskDependency>()
            .HasOne(d => d.Task)
            .WithMany(t => t.Dependencies)
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TaskDependency>()
            .HasOne(d => d.DependsOnTask)
            .WithMany(t => t.DependentTasks)
            .HasForeignKey(d => d.DependsOnTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaskDependency>()
            .HasIndex(d => new { d.TaskId, d.DependsOnTaskId })
            .IsUnique();

        builder.Entity<TaskTemplate>()
            .HasIndex(t => new { t.OwnerId, t.Name });

        builder.Entity<Team>()
            .HasOne(t => t.Owner)
            .WithMany(u => u.OwnedTeams)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamMember>()
            .HasIndex(tm => new { tm.TeamId, tm.UserId })
            .IsUnique();

        builder.Entity<TeamMember>()
            .HasOne(tm => tm.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamMember>()
            .HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamInvitation>()
            .HasIndex(i => i.Token)
            .IsUnique();

        builder.Entity<TeamInvitation>()
            .HasIndex(i => new { i.TeamId, i.Email, i.IsAccepted });

        builder.Entity<TeamInvitation>()
            .HasOne(i => i.Team)
            .WithMany(t => t.Invitations)
            .HasForeignKey(i => i.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamInvitation>()
            .HasOne(i => i.InvitedBy)
            .WithMany(u => u.SentTeamInvitations)
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamActivityLog>()
            .HasOne(t => t.Team)
            .WithMany(t => t.ActivityLogs)
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamActivityLog>()
            .HasOne(t => t.ActorUser)
            .WithMany(u => u.TeamActivities)
            .HasForeignKey(t => t.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TeamActivityLog>()
            .HasIndex(t => new { t.TeamId, t.CreatedAtUtc });

        builder.Entity<TaskComment>()
            .HasOne(c => c.TaskItem)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TaskComment>()
            .HasOne(c => c.AuthorUser)
            .WithMany(u => u.TaskComments)
            .HasForeignKey(c => c.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CommentReaction>()
            .HasOne(r => r.TaskComment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(r => r.TaskCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CommentReaction>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CommentReaction>()
            .HasIndex(r => new { r.TaskCommentId, r.UserId, r.Emoji })
            .IsUnique();

        builder.Entity<FileAttachment>()
            .HasOne(f => f.TaskItem)
            .WithMany(t => t.Attachments)
            .HasForeignKey(f => f.TaskItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FileAttachment>()
            .HasOne(f => f.TaskComment)
            .WithMany(c => c.Attachments)
            .HasForeignKey(f => f.TaskCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FileAttachment>()
            .HasOne(f => f.UploadedByUser)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FileAttachment>()
            .HasIndex(f => new { f.TaskItemId, f.FileName, f.Version });

        builder.Entity<TimeEntry>()
            .HasOne(t => t.TaskItem)
            .WithMany(t => t.TimeEntries)
            .HasForeignKey(t => t.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TimeEntry>()
            .HasOne(t => t.User)
            .WithMany(u => u.TimeEntries)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TimeEntry>()
            .HasIndex(t => new { t.TaskItemId, t.StartedAtUtc });

        builder.Entity<TimeEntry>()
            .HasIndex(t => new { t.UserId, t.EndedAtUtc });

        builder.Entity<LoginActivity>()
            .HasOne(a => a.User)
            .WithMany(u => u.LoginActivities)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LoginActivity>()
            .HasIndex(a => new { a.UserId, a.OccurredAtUtc });

        builder.Entity<DeviceSession>()
            .HasOne(s => s.User)
            .WithMany(u => u.DeviceSessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DeviceSession>()
            .HasIndex(s => s.SessionKey)
            .IsUnique();

        builder.Entity<DeviceSession>()
            .HasIndex(s => new { s.UserId, s.RevokedAtUtc });

        builder.Entity<NotificationItem>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<NotificationItem>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAtUtc });

        builder.Entity<UserInvitation>()
            .HasOne(i => i.InvitedBy)
            .WithMany(u => u.SentUserInvitations)
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserInvitation>()
            .HasIndex(i => i.Token)
            .IsUnique();

        builder.Entity<UserInvitation>()
            .HasIndex(i => new { i.Email, i.IsAccepted });
    }
}
