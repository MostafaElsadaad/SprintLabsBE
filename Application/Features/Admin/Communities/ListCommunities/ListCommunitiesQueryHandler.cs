using Application.Features.Admin.Communities.Common;

using Domain.Repositories;
using Domain.Services;

using MediatR;

namespace Application.Features.Admin.Communities.ListCommunities;

public class ListCommunitiesQueryHandler : IRequestHandler<ListCommunitiesQuery, List<CommunityListItemResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityRepository _communityRepository;

    public ListCommunitiesQueryHandler(
        IUserService userService,
        ICommunityRepository communityRepository)
    {
        _userService = userService;
        _communityRepository = communityRepository;
    }

    public async Task<List<CommunityListItemResponse>> Handle(ListCommunitiesQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(_userService, request.AuthenticatedUserId);

        var communities = await _communityRepository.ListCommunitiesAsync();

        return communities.Select(x => new CommunityListItemResponse
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Status = x.Status,
            Owner = x.Owner == null
                ? null
                : new OwnerSummaryResponse
                {
                    UserId = x.Owner.UserId,
                    Email = x.Owner.Email,
                    Name = x.Owner.Name,
                    Status = x.Owner.Status
                },
            License = x.License == null
                ? null
                : new CommunityLicenseSummaryResponse
                {
                    MaxStudents = x.License.MaxStudents,
                    UsedStudents = x.License.UsedStudents,
                    MaxTeachers = x.License.MaxTeachers,
                    UsedTeachers = x.License.UsedTeachers,
                    StudentEmailChangeLimit = x.License.StudentEmailChangeLimit
                }
        }).ToList();
    }
}
