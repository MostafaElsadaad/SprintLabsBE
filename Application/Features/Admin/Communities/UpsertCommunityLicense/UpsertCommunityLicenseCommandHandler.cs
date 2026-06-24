using System.Net;

using Application.Features.Admin.Communities.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.UpsertCommunityLicense;

public class UpsertCommunityLicenseCommandHandler : IRequestHandler<UpsertCommunityLicenseCommand, CommunityLicenseResponse>
{
    private readonly IUserService _userService;
    private readonly IBaseRepository<Community> _communityRepository;
    private readonly IBaseRepository<CommunityLicense> _communityLicenseRepository;

    public UpsertCommunityLicenseCommandHandler(
        IUserService userService,
        IBaseRepository<Community> communityRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository)
    {
        _userService = userService;
        _communityRepository = communityRepository;
        _communityLicenseRepository = communityLicenseRepository;
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

        if (!await _communityRepository.AsQueryable().AnyAsync(x => x.Id == request.CommunityId, cancellationToken))
        {
            throw NotFound();
        }

        var license = await _communityLicenseRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.CommunityId == request.CommunityId, cancellationToken);
        if (license != null &&
            (request.MaxStudents < license.UsedStudents ||
             request.MaxTeachers < license.UsedTeachers))
        {
            throw InvalidInput();
        }

        if (license == null)
        {
            license = new CommunityLicense
            {
                CommunityId = request.CommunityId,
                MaxStudents = request.MaxStudents,
                UsedStudents = 0,
                MaxTeachers = request.MaxTeachers,
                UsedTeachers = 0,
                StudentEmailChangeLimit = request.StudentEmailChangeLimit,
                CreatedAt = DateTime.UtcNow
            };
            await _communityLicenseRepository.AddAsync(license);
        }
        else
        {
            license.MaxStudents = request.MaxStudents;
            license.MaxTeachers = request.MaxTeachers;
            license.StudentEmailChangeLimit = request.StudentEmailChangeLimit;
            license.UpdatedAt = DateTime.UtcNow;
            await _communityLicenseRepository.UpdateAsync(license);
        }

        await _communityLicenseRepository.SaveChangesAsync();

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
