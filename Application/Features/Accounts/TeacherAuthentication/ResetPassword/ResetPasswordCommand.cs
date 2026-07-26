using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.ResetPassword;

public class ResetPasswordCommand : IRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
