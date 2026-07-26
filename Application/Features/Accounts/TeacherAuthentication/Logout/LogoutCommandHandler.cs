using Domain.Services;
using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;
    public LogoutCommandHandler(IRefreshTokenService refreshTokenService) => _refreshTokenService = refreshTokenService;
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken) => _refreshTokenService.RevokeAsync(request.RefreshToken, request.RevokedByIp, cancellationToken);
}
