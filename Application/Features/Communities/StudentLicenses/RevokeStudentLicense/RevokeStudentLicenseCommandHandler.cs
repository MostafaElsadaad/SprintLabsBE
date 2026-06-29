using System.Net;

using Application.Features.Communities.StudentLicenses.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.StudentLicenses.RevokeStudentLicense;

public class RevokeStudentLicenseCommandHandler
    : IRequestHandler<RevokeStudentLicenseCommand, StudentLicenseResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<CommunityLicense> _communityLicenseRepository;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;

    public RevokeStudentLicenseCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository,
        IBaseRepository<CommunityUser> communityUserRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _studentLicenseRepository = studentLicenseRepository;
        _communityLicenseRepository = communityLicenseRepository;
        _communityUserRepository = communityUserRepository;
    }

    public async Task<StudentLicenseResponse> Handle(
        RevokeStudentLicenseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0 || request.LicenseId <= 0)
        {
            throw InvalidInput();
        }

        await StudentLicenseAuthorization.EnsureActiveOwner(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        var studentLicense = await _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(
                x => x.Id == request.LicenseId && x.CommunityId == request.CommunityId,
                cancellationToken);
        if (studentLicense == null)
        {
            throw NotFound();
        }

        if (studentLicense.Status == StudentLicenseStatus.Revoked)
        {
            return await StudentLicenseMapper.Map(studentLicense, _userService);
        }

        var communityLicense = await _communityLicenseRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.CommunityId == request.CommunityId, cancellationToken);
        if (communityLicense == null)
        {
            throw NotFound();
        }

        var wasActive = studentLicense.Status == StudentLicenseStatus.Active;
        studentLicense.Status = StudentLicenseStatus.Revoked;
        studentLicense.UpdatedAt = DateTime.UtcNow;

        if (communityLicense.UsedStudents > 0)
        {
            communityLicense.UsedStudents--;
            communityLicense.UpdatedAt = DateTime.UtcNow;
            await _communityLicenseRepository.UpdateAsync(communityLicense);
        }

        if (wasActive && studentLicense.UserId.HasValue)
        {
            var membership = await _communityUserRepository.AsQueryable()
                .FirstOrDefaultAsync(
                    x => x.CommunityId == request.CommunityId
                         && x.UserId == studentLicense.UserId.Value
                         && x.Role == CommunityUserRole.Student,
                    cancellationToken);
            if (membership != null && membership.Status != CommunityUserStatus.Removed)
            {
                membership.Status = CommunityUserStatus.Removed;
                membership.UpdatedAt = DateTime.UtcNow;
                await _communityUserRepository.UpdateAsync(membership);
            }
        }

        await _studentLicenseRepository.UpdateAsync(studentLicense);
        await _studentLicenseRepository.SaveChangesAsync();

        return await StudentLicenseMapper.Map(studentLicense, _userService);
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
