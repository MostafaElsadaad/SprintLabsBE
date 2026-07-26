namespace Application.Features.Accounts.TeacherAuthentication.ConfirmEmail;

public class ConfirmEmailRequest
{
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
