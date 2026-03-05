namespace TaskFlowMvc.Models;

public enum TeamActivityType
{
    TeamCreated = 0,
    LeaderAssigned = 1,
    MemberAdded = 2,
    MemberRemoved = 3,
    LeaderTransferred = 4,
    InvitationSent = 5,
    InvitationAccepted = 6,
    InvitationRevoked = 7,
    ProjectAssigned = 8
}
