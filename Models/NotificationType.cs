namespace TaskFlowMvc.Models;

public enum NotificationType
{
    TaskAssigned = 0,
    TaskUpdated = 1,
    CommentAdded = 2,
    TaskOverdue = 3,
    MentionedInComment = 4,
    TeamActivity = 5,
    Security = 6
}
