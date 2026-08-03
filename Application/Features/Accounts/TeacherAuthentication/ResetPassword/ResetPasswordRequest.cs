namespace Application.Features.Accounts.TeacherAuthentication.ResetPassword;

public class ResetPasswordRequest
{
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}