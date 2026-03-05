namespace TaskFlowMvc.Models;

public enum LoginActivityType
{
    LoginSucceeded = 0,
    LoginFailed = 1,
    TwoFactorChallengeSent = 2,
    TwoFactorSucceeded = 3,
    TwoFactorFailed = 4,
    PasswordResetRequested = 5,
    PasswordResetCompleted = 6,
    Logout = 7
}
