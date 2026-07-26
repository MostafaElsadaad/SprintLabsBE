using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.Logout;

public class LogoutCommand : IRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? RevokedByIp { get; set; }
}
