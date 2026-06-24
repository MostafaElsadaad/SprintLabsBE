using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Users.GetCurrentUserCommunities;

public class GetCurrentUserCommunitiesQueryHandler : IRequestHandler<GetCurrentUserCommunitiesQuery, List<UserCommunityResponse>>
{
    private readonly IUserService _userService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;

    public GetCurrentUserCommunitiesQueryHandler(
        IUserService userService,
        IBaseRepository<CommunityUser> communityUserRepository)
    {
        _userService = userService;
        _communityUserRepository = communityUserRepository;
    }

    public async Task<List<UserCommunityResponse>> Handle(
        GetCurrentUserCommunitiesQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetCurrentUser(request.UserId);
        if (user == null)
        {
            throw new GenericException(
                message: ErrorMessage.NotFound,
                statusCode: HttpStatusCode.NotFound,
                errorCode: ErrorCode.Failure);
        }

        if (user.IsSuspended)
        {
            throw new GenericException(
                message: ErrorMessage.InvalidAccessToken,
                statusCode: HttpStatusCode.Forbidden,
                errorCode: ErrorCode.Failure);
        }

        return await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == request.UserId && x.Status == CommunityUserStatus.Active)
            .OrderBy(x => x.Community.Name)
            .Select(x => new UserCommunityResponse
            {
                CommunityId = x.CommunityId,
                CommunityName = x.Community.Name,
                Slug = x.Community.Slug,
                CommunityStatus = x.Community.Status.ToString(),
                Role = x.Role.ToString(),
                MembershipStatus = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
