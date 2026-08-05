using System.Net;

using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.TeacherAuthentication.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TeacherTokenResponse>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccessTokenService _accessTokenService;
    public RefreshTokenCommandHandler(IRefreshTokenService refreshTokenService, IAccessTokenService accessTokenService)
    { _refreshTokenService = refreshTokenService; _accessTokenService = accessTokenService; }

    public async Task<TeacherTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refresh = await _refreshTokenService.RotateAsync(request.RefreshToken, request.RevokedByIp, cancellationToken);
        if (refresh == null) throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        var access = _accessTokenService.Create(refresh.UserId, refresh.Email, refresh.Name, refresh.AccountType);
        return new TeacherTokenResponse { AccessToken = access.AccessToken, AccessTokenExpiresAt = access.AccessTokenExpiresAt, RefreshToken = refresh.RefreshToken, RefreshTokenExpiresAt = refresh.RefreshTokenExpiresAt };
    }
}
