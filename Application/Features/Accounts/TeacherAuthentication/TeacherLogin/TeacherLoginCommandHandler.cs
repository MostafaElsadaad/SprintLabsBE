using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Shared.Responses;

namespace Application.Features.Accounts.TeacherAuthentication.TeacherLogin;

public class TeacherLoginCommandHandler : IRequestHandler<TeacherLoginCommand, TeacherLoginResponse>
{
    private readonly ITeacherIdentityService _identityService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly ILogger<TeacherLoginCommandHandler> _logger;
    public TeacherLoginCommandHandler(ITeacherIdentityService identityService, IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService, IBaseRepository<CommunityUser> communityUserRepository, ILogger<TeacherLoginCommandHandler> logger)
    {
        _identityService = identityService; _accessTokenService = accessTokenService; _refreshTokenService = refreshTokenService; _communityUserRepository = communityUserRepository; _logger = logger;
    }

    public async Task<TeacherLoginResponse> Handle(TeacherLoginCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _identityService.AuthenticateAsync(request.Identifier, request.Password, cancellationToken);
        var memberships = await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == teacher.UserId && x.Role == CommunityUserRole.Teacher &&
                        (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending))
            .Select(x => new { x.CommunityId, x.Status, CommunityName = x.Community.Name, CommunityStatus = x.Community.Status })
            .ToListAsync(cancellationToken);

        var activeMemberships = memberships
            .Where(x => x.Status == CommunityUserStatus.Active && x.CommunityStatus == CommunityStatus.Active)
            .ToList();
        if (activeMemberships.Count != 1 || memberships.Any(x => x.Status == CommunityUserStatus.Pending))
        {
            if (memberships.Count > 1)
            {
                _logger.LogWarning("Teacher {TeacherUserId} has inconsistent current community memberships.", teacher.UserId);
            }
            throw new Shared.Exceptions.GenericException(Shared.Enums.ErrorCode.Failure, Shared.Enums.ErrorMessage.InvalidAccessToken, System.Net.HttpStatusCode.Unauthorized);
        }

        var activeMembership = activeMemberships[0];
        var access = _accessTokenService.Create(teacher.UserId, teacher.Email, teacher.Name);
        var refresh = await _refreshTokenService.IssueAsync(teacher.UserId, request.CreatedByIp, cancellationToken);
        return new TeacherLoginResponse
        {
            UserId = teacher.UserId,
            Name = teacher.Name,
            Email = teacher.Email,
            AccessToken = access.AccessToken,
            AccessTokenExpiresAt = access.AccessTokenExpiresAt,
            RefreshToken = refresh.RefreshToken,
            RefreshTokenExpiresAt = refresh.RefreshTokenExpiresAt,
            Community = new TeacherCommunityResponse { Id = activeMembership.CommunityId, Name = activeMembership.CommunityName }
        };
    }
}