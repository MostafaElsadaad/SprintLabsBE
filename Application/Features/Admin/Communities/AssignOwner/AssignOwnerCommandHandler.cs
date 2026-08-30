using System.Net;

using Application.Features.Admin.Communities.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.AssignOwner;

public class AssignOwnerCommandHandler : IRequestHandler<AssignOwnerCommand, OwnerResponse>
{
    private readonly IUserService _userService;
    private readonly IBaseRepository<Community> _communityRepository;
    private readonly IStaffCommunityMembershipService _staffCommunityMembershipService;

    public AssignOwnerCommandHandler(
        IUserService userService,
        IBaseRepository<Community> communityRepository,
        IStaffCommunityMembershipService staffCommunityMembershipService)
    {
        _userService = userService;
        _communityRepository = communityRepository;
        _staffCommunityMembershipService = staffCommunityMembershipService;
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

        if (!await _communityRepository.AsQueryable().AnyAsync(x => x.Id == request.CommunityId, cancellationToken))
        {
            throw NotFound();
        }

        var ownerUser = await _userService.FindOrCreateBasicUser(request.Email, request.Name);
        var owner = await _staffCommunityMembershipService.AssignOwnerAsync(
            ownerUser.Id,
            request.CommunityId,
            cancellationToken);

        return new OwnerResponse
        {
            CommunityUserId = owner.Id,
            CommunityId = owner.CommunityId,
            UserId = ownerUser.Id,
            Email = ownerUser.Email,
            Name = ownerUser.Name,
            Role = owner.Role.ToString(),
            Status = owner.Status.ToString()
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
