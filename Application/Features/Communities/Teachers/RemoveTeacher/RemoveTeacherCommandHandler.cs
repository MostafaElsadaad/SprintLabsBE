using System.Net;

using Application.Features.Communities.Teachers.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.Teachers.RemoveTeacher;

public class RemoveTeacherCommandHandler : IRequestHandler<RemoveTeacherCommand, TeacherResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly IBaseRepository<CommunityLicense> _communityLicenseRepository;
    private readonly ITeacherInvitationService _teacherInvitationService;

    public RemoveTeacherCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<CommunityUser> communityUserRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository,
        ITeacherInvitationService teacherInvitationService)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityUserRepository = communityUserRepository;
        _communityLicenseRepository = communityLicenseRepository;
        _teacherInvitationService = teacherInvitationService;
    }

    public RemoveTeacherCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<CommunityUser> communityUserRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityUserRepository = communityUserRepository;
        _communityLicenseRepository = communityLicenseRepository;
        _teacherInvitationService = null!;
    }

    public async Task<TeacherResponse> Handle(
        RemoveTeacherCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            request.TeacherUserId <= 0)
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

        var membership = await _communityUserRepository.AsQueryable()
            .FirstOrDefaultAsync(
                x => x.CommunityId == request.CommunityId && x.UserId == request.TeacherUserId,
                cancellationToken);
        if (membership == null)
        {
            throw NotFound();
        }

        if (membership.Role != CommunityUserRole.Teacher)
        {
            throw InvalidInput();
        }

        var teacher = await _userService.GetCurrentUser(request.TeacherUserId);
        if (teacher == null)
        {
            throw NotFound();
        }

        if (membership.Status == CommunityUserStatus.Removed)
        {
            return Map(teacher, membership);
        }

        var license = await _communityLicenseRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.CommunityId == request.CommunityId, cancellationToken);
        if (license == null)
        {
            throw NotFound();
        }

        membership.Status = CommunityUserStatus.Removed;
        membership.UpdatedAt = DateTime.UtcNow;

        if (license.UsedTeachers > 0)
        {
            license.UsedTeachers--;
            license.UpdatedAt = DateTime.UtcNow;
            await _communityLicenseRepository.UpdateAsync(license);
        }

        await _communityUserRepository.UpdateAsync(membership);
        await _communityUserRepository.SaveChangesAsync();
        if (_teacherInvitationService != null) await _teacherInvitationService.RevokeForMembershipAsync(membership.Id, cancellationToken);

        return Map(teacher, membership);
    }

    private static TeacherResponse Map(Shared.Responses.UserIdentityResponse user, CommunityUser membership)
    {
        return new TeacherResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Status = membership.Status.ToString(),
            CreatedAt = membership.CreatedAt
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
