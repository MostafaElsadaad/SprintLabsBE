using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.ForgotPassword;

public class ForgotPasswordCommand : IRequest
{
    public string Email { get; set; } = string.Empty;
}
