namespace Shared.Options;

public class TeacherAuthenticationOptions
{
    public int ConfirmationTokenLifetimeHours { get; set; } = 24;
    public int PasswordResetTokenLifetimeHours { get; set; } = 1;
    public int ConfirmationResendCooldownSeconds { get; set; } = 60;
    public int InvitationLifetimeDays { get; set; } = 7;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
