using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.CommunityAuthentication.CommunityLogin;

public class CommunityLoginCommandHandler : IRequestHandler<CommunityLoginCommand, CommunityLoginResponse>
{
    private readonly ITeacherIdentityService _identityService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly ILogger<CommunityLoginCommandHandler> _logger;

    public CommunityLoginCommandHandler(
        ITeacherIdentityService identityService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IBaseRepository<CommunityUser> communityUserRepository,
        ILogger<CommunityLoginCommandHandler> logger)
    {
        _identityService = identityService;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _communityUserRepository = communityUserRepository;
        _logger = logger;
    }

    public async Task<CommunityLoginResponse> Handle(CommunityLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.AuthenticateAsync(request.Identifier, request.Password, cancellationToken);
        var memberships = await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == user.UserId &&
                        (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                        (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active))
            .Select(x => new { x.CommunityId, x.Role, x.Status, CommunityName = x.Community.Name, CommunityStatus = x.Community.Status })
            .ToListAsync(cancellationToken);
        if (memberships.Select(x => x.CommunityId).Distinct().Count() > 1)
        {
            _logger.LogError("Community login data conflict for user {CommunityUserId}.", user.UserId);
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Conflict);
        }

        if (user.IsPlatformAdmin)
        {
            return await CreateResponseAsync(user, AuthenticatedAccountType.PlatformAdmin, null, request.CreatedByIp, cancellationToken);
        }

        var activeMemberships = memberships
            .Where(x => x.Status == CommunityUserStatus.Active && x.CommunityStatus == CommunityStatus.Active)
            .ToList();

        if (activeMemberships.Count > 1)
        {
            _logger.LogError("Community login data conflict for user {CommunityUserId}.", user.UserId);
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Conflict);
        }

        if (activeMemberships.Count != 1 || memberships.Any(x => x.Status == CommunityUserStatus.Pending))
        {
            throw InvalidCredentials();
        }

        var membership = activeMemberships[0];
        var accountType = membership.Role == CommunityUserRole.Owner
            ? AuthenticatedAccountType.CommunityAdmin
            : AuthenticatedAccountType.Teacher;
        return await CreateResponseAsync(
            user,
            accountType,
            new TeacherCommunityResponse { Id = membership.CommunityId, Name = membership.CommunityName },
            request.CreatedByIp,
            cancellationToken);
    }

    private async Task<CommunityLoginResponse> CreateResponseAsync(
        TeacherIdentityResult user,
        AuthenticatedAccountType accountType,
        TeacherCommunityResponse? community,
        string? createdByIp,
        CancellationToken cancellationToken)
    {
        var access = _accessTokenService.Create(user.UserId, user.Email, user.Name, accountType);
        var refresh = await _refreshTokenService.IssueAsync(user.UserId, createdByIp, cancellationToken);

        return new CommunityLoginResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Username = user.Username,
            Email = user.Email,
            Role = accountType switch
            {
                AuthenticatedAccountType.PlatformAdmin => "PlatformAdmin",
                AuthenticatedAccountType.CommunityAdmin => "CommunityAdmin",
                _ => "Teacher"
            },
            Community = community,
            AccessToken = access.AccessToken,
            AccessTokenExpiresAt = access.AccessTokenExpiresAt,
            RefreshToken = refresh.RefreshToken,
            RefreshTokenExpiresAt = refresh.RefreshTokenExpiresAt
        };
    }

    private static GenericException InvalidCredentials() => new(
        ErrorCode.Failure,
        ErrorMessage.InvalidAccessToken,
        HttpStatusCode.Unauthorized);
}
