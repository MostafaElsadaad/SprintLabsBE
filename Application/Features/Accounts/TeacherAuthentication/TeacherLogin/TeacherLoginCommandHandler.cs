using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Responses;

namespace Application.Features.Accounts.TeacherAuthentication.TeacherLogin;

public class TeacherLoginCommandHandler : IRequestHandler<TeacherLoginCommand, TeacherLoginResponse>
{
    private readonly ITeacherIdentityService _identityService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    public TeacherLoginCommandHandler(ITeacherIdentityService identityService, IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService, IBaseRepository<CommunityUser> communityUserRepository)
    {
        _identityService = identityService; _accessTokenService = accessTokenService; _refreshTokenService = refreshTokenService; _communityUserRepository = communityUserRepository;
    }

    public async Task<TeacherLoginResponse> Handle(TeacherLoginCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _identityService.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        var access = _accessTokenService.Create(teacher.UserId, teacher.Email, teacher.Name);
        var refresh = await _refreshTokenService.IssueAsync(teacher.UserId, request.CreatedByIp, cancellationToken);
        var communities = await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == teacher.UserId && x.Status == CommunityUserStatus.Active)
            .OrderBy(x => x.Community.Name)
            .Select(x => new TeacherCommunityResponse { CommunityId = x.CommunityId, CommunityName = x.Community.Name, Role = x.Role.ToString() })
            .ToListAsync(cancellationToken);
        return new TeacherLoginResponse
        {
            UserId = teacher.UserId, Name = teacher.Name, Email = teacher.Email,
            AccessToken = access.AccessToken, AccessTokenExpiresAt = access.AccessTokenExpiresAt,
            RefreshToken = refresh.RefreshToken, RefreshTokenExpiresAt = refresh.RefreshTokenExpiresAt,
            Communities = communities
        };
    }
}
