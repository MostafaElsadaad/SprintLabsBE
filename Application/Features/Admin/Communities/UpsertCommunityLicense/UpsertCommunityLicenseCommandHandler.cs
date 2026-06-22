using System.Net;

using Application.Features.Admin.Communities.Common;

using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.UpsertCommunityLicense;

public class UpsertCommunityLicenseCommandHandler : IRequestHandler<UpsertCommunityLicenseCommand, CommunityLicenseResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityRepository _communityRepository;

    public UpsertCommunityLicenseCommandHandler(
        IUserService userService,
        ICommunityRepository communityRepository)
    {
        _userService = userService;
        _communityRepository = communityRepository;
    }

    public async Task<CommunityLicenseResponse> Handle(UpsertCommunityLicenseCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(_userService, request.AuthenticatedUserId);

        if (request.CommunityId <= 0 ||
            request.MaxStudents < 0 ||
            request.MaxTeachers < 0 ||
            request.StudentEmailChangeLimit < 0)
        {
            throw InvalidInput();
        }

        if (!await _communityRepository.CommunityExistsAsync(request.CommunityId))
        {
            throw NotFound();
        }

        var existingLicense = await _communityRepository.GetLicenseByCommunityIdAsync(request.CommunityId);
        if (existingLicense != null &&
            (request.MaxStudents < existingLicense.UsedStudents ||
             request.MaxTeachers < existingLicense.UsedTeachers))
        {
            throw InvalidInput();
        }

        var license = await _communityRepository.UpsertLicenseAsync(
            request.CommunityId,
            request.MaxStudents,
            request.MaxTeachers,
            request.StudentEmailChangeLimit);

        return new CommunityLicenseResponse
        {
            Id = license.Id,
            CommunityId = license.CommunityId,
            MaxStudents = license.MaxStudents,
            UsedStudents = license.UsedStudents,
            MaxTeachers = license.MaxTeachers,
            UsedTeachers = license.UsedTeachers,
            StudentEmailChangeLimit = license.StudentEmailChangeLimit
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
