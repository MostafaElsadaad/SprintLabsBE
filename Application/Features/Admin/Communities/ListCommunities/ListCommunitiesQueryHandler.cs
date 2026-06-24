using Application.Features.Admin.Communities.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.Communities.ListCommunities;

public class ListCommunitiesQueryHandler : IRequestHandler<ListCommunitiesQuery, List<CommunityListItemResponse>>
{
    private readonly IUserService _userService;
    private readonly IBaseRepository<Community> _communityRepository;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;

    public ListCommunitiesQueryHandler(
        IUserService userService,
        IBaseRepository<Community> communityRepository,
        IBaseRepository<CommunityUser> communityUserRepository)
    {
        _userService = userService;
        _communityRepository = communityRepository;
        _communityUserRepository = communityUserRepository;
    }

    public async Task<List<CommunityListItemResponse>> Handle(ListCommunitiesQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(_userService, request.AuthenticatedUserId);

        var communities = await _communityRepository.AsQueryable()
            .Include(x => x.License)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var communityIds = communities.Select(x => x.Id).ToList();
        var ownerMemberships = await _communityUserRepository.AsQueryable()
            .Where(x =>
                communityIds.Contains(x.CommunityId)
                && x.Role == CommunityUserRole.Owner
                && x.Status == CommunityUserStatus.Active)
            .ToListAsync(cancellationToken);

        var ownerUsers = new Dictionary<long, Shared.Responses.UserIdentityResponse>();
        foreach (var ownerUserId in ownerMemberships.Select(x => x.UserId).Distinct())
        {
            var user = await _userService.GetCurrentUser(ownerUserId);
            if (user != null)
            {
                ownerUsers[user.Id] = user;
            }
        }

        var ownersByCommunity = ownerMemberships
            .Where(x => ownerUsers.ContainsKey(x.UserId))
            .GroupBy(x => x.CommunityId)
            .ToDictionary(
                x => x.Key,
                x =>
                {
                    var membership = x.First();
                    var user = ownerUsers[membership.UserId];

                    return new OwnerSummaryResponse
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Status = membership.Status.ToString()
                    };
                });

        return communities.Select(x => new CommunityListItemResponse
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Status = x.Status.ToString(),
            Owner = ownersByCommunity.TryGetValue(x.Id, out var owner) ? owner : null,
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
