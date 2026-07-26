using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.ConfirmEmail;

public class ConfirmEmailCommand : IRequest
{
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
