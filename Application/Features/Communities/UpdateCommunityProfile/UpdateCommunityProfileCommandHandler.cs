using System.Net;

using Application.Features.Communities.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.UpdateCommunityProfile;

public class UpdateCommunityProfileCommandHandler
    : IRequestHandler<UpdateCommunityProfileCommand, CommunityProfileResponse>
{
    private const int NameMaxLength = 200;
    private const int SlugMaxLength = 120;

    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Community> _communityRepository;

    public UpdateCommunityProfileCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<Community> communityRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityRepository = communityRepository;
    }

    public async Task<CommunityProfileResponse> Handle(
        UpdateCommunityProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CommunityId <= 0 ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Slug))
        {
            throw InvalidInput();
        }

        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (name.Length > NameMaxLength || slug.Length > SlugMaxLength)
        {
            throw InvalidInput();
        }

        var user = await _userService.GetCurrentUser(request.UserId);
        if (user == null)
        {
            throw NotFound();
        }

        if (user.IsSuspended)
        {
            throw Forbidden();
        }

        var isOwner = await _communityAccessService.HasCommunityRole(
            request.UserId,
            request.CommunityId,
            new[] { CommunityUserRole.Owner });
        if (!isOwner)
        {
            throw Forbidden();
        }

        var community = await _communityRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.CommunityId, cancellationToken);
        if (community == null)
        {
            throw NotFound();
        }

        var duplicateSlug = await _communityRepository.AsQueryable()
            .AnyAsync(
                x => x.Id != request.CommunityId && x.Slug == slug,
                cancellationToken);
        if (duplicateSlug)
        {
            throw new GenericException(
                message: ErrorMessage.ExistingRecord,
                statusCode: HttpStatusCode.BadRequest,
                errorCode: ErrorCode.Failure);
        }

        community.Name = name;
        community.Slug = slug;
        community.UpdatedAt = DateTime.UtcNow;

        await _communityRepository.UpdateAsync(community);
        await _communityRepository.SaveChangesAsync();

        return new CommunityProfileResponse
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

    private static GenericException Forbidden()
    {
        return new GenericException(
            message: ErrorMessage.InvalidAccessToken,
            statusCode: HttpStatusCode.Forbidden,
            errorCode: ErrorCode.Failure);
    }

    private static GenericException NotFound()
    {
        return new GenericException(
            message: ErrorMessage.NotFound,
            statusCode: HttpStatusCode.NotFound,
            errorCode: ErrorCode.Failure);
    }
}
