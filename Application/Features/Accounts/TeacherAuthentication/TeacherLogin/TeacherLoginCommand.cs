using MediatR;
using Shared.Responses;

namespace Application.Features.Accounts.TeacherAuthentication.TeacherLogin;

public class TeacherLoginCommand : IRequest<TeacherLoginResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? CreatedByIp { get; set; }
}
