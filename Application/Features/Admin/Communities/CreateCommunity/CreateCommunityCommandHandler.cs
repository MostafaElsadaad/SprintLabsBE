using System.Net;

using Application.Features.Admin.Communities.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.CreateCommunity;

public class CreateCommunityCommandHandler : IRequestHandler<CreateCommunityCommand, CommunityResponse>
{
    private readonly IUserService _userService;
    private readonly IBaseRepository<Community> _communityRepository;

    public CreateCommunityCommandHandler(
        IUserService userService,
        IBaseRepository<Community> communityRepository)
    {
        _userService = userService;
        _communityRepository = communityRepository;
    }

    public async Task<CommunityResponse> Handle(CreateCommunityCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(_userService, request.AuthenticatedUserId);

        var name = request.Name.Trim();
        var slug = NormalizeSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
        {
            throw InvalidInput();
        }

        if (await _communityRepository.AsQueryable().AnyAsync(x => x.Slug == slug))
        {
            throw new GenericException(
                message: ErrorMessage.ExistingRecord,
                statusCode: HttpStatusCode.BadRequest,
                errorCode: ErrorCode.Failure);
        }

        var community = await _communityRepository.AddAsync(new Community
        {
            Name = name,
            Slug = slug,
            Status = CommunityStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _communityRepository.SaveChangesAsync();

        return MapCommunity(community);
    }

    private static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }

    private static CommunityResponse MapCommunity(Community community)
    {
        return new CommunityResponse
        {
            Id = community.Id,
            Name = community.Name,
            Slug = community.Slug,
            Status = community.Status.ToString()
        };
    }

    private static GenericException InvalidInput()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.BadRequest,
            errorCode: ErrorCode.Failure);
    }
}
