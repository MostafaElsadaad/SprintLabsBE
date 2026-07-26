using MediatR;
using Shared.Responses;

namespace Application.Features.Accounts.TeacherAuthentication.RefreshToken;

public class RefreshTokenCommand : IRequest<TeacherTokenResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? RevokedByIp { get; set; }
}
