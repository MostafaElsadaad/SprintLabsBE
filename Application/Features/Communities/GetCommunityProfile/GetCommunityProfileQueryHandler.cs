using System.Net;

using Application.Features.Communities.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.GetCommunityProfile;

public class GetCommunityProfileQueryHandler : IRequestHandler<GetCommunityProfileQuery, CommunityProfileResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Community> _communityRepository;

    public GetCommunityProfileQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<Community> communityRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityRepository = communityRepository;
    }

    public async Task<CommunityProfileResponse> Handle(
        GetCommunityProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CommunityId <= 0)
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

        if (!await _communityAccessService.CanAccessCommunity(request.UserId, request.CommunityId))
        {
            throw Forbidden();
        }

        var community = await _communityRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.CommunityId, cancellationToken);
        if (community == null)
        {
            throw NotFound();
        }

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
