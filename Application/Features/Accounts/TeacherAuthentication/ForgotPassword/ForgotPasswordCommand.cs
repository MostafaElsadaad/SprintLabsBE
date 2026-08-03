using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.ForgotPassword;

public class ForgotPasswordCommand : IRequest
{
    public string Identifier { get; set; } = string.Empty;
}