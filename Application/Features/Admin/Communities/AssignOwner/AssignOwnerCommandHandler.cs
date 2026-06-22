using System.Net;

using Application.Features.Admin.Communities.Common;

using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.AssignOwner;

public class AssignOwnerCommandHandler : IRequestHandler<AssignOwnerCommand, OwnerResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityRepository _communityRepository;

    public AssignOwnerCommandHandler(
        IUserService userService,
        ICommunityRepository communityRepository)
    {
        _userService = userService;
        _communityRepository = communityRepository;
    }

    public async Task<OwnerResponse> Handle(AssignOwnerCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(_userService, request.AuthenticatedUserId);

        if (request.CommunityId <= 0 ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            throw InvalidInput();
        }

        if (!await _communityRepository.CommunityExistsAsync(request.CommunityId))
        {
            throw NotFound();
        }

        var ownerUser = await _userService.FindOrCreateBasicUser(request.Email, request.Name);
        var owner = await _communityRepository.UpsertOwnerAsync(request.CommunityId, ownerUser.Id);

        return new OwnerResponse
        {
            CommunityUserId = owner.CommunityUserId,
            CommunityId = owner.CommunityId,
            UserId = owner.UserId,
            Email = owner.Email,
            Name = owner.Name,
            Role = owner.Role,
            Status = owner.Status
        };
    }

    private static GenericException InvalidInput()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.BadRequest,
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
