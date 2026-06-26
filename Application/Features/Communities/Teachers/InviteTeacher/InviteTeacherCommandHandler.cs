using System.Net;
using System.Net.Mail;

using Application.Features.Communities.Teachers.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Communities.Teachers.InviteTeacher;

public class InviteTeacherCommandHandler : IRequestHandler<InviteTeacherCommand, TeacherResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly IBaseRepository<CommunityLicense> _communityLicenseRepository;

    public InviteTeacherCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<CommunityUser> communityUserRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityUserRepository = communityUserRepository;
        _communityLicenseRepository = communityLicenseRepository;
    }

    public async Task<TeacherResponse> Handle(
        InviteTeacherCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            throw InvalidInput();
        }

        var email = NormalizeEmail(request.Email);
        var name = request.Name.Trim();
        if (!IsValidEmail(email))
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

        if (!await IsActiveOwner(request.UserId, request.CommunityId))
        {
            throw Forbidden();
        }

        var teacherUser = await _userService.FindOrCreateBasicUser(email, name);
        var membership = await _communityUserRepository.AsQueryable()
            .FirstOrDefaultAsync(
                x => x.CommunityId == request.CommunityId && x.UserId == teacherUser.Id,
                cancellationToken);

        if (membership != null && membership.Role != CommunityUserRole.Teacher)
        {
            throw InvalidInput();
        }

        if (membership is { Status: CommunityUserStatus.Pending or CommunityUserStatus.Active })
        {
            return Map(teacherUser, membership);
        }

        var license = await _communityLicenseRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.CommunityId == request.CommunityId, cancellationToken);
        if (license == null || license.UsedTeachers >= license.MaxTeachers)
        {
            throw InvalidInput();
        }

        if (membership == null)
        {
            membership = new CommunityUser
            {
                CommunityId = request.CommunityId,
                UserId = teacherUser.Id,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _communityUserRepository.AddAsync(membership);
        }
        else
        {
            membership.Status = CommunityUserStatus.Pending;
            membership.UpdatedAt = DateTime.UtcNow;
            await _communityUserRepository.UpdateAsync(membership);
        }

        license.UsedTeachers++;
        license.UpdatedAt = DateTime.UtcNow;
        await _communityLicenseRepository.UpdateAsync(license);
        await _communityUserRepository.SaveChangesAsync();

        return Map(teacherUser, membership);
    }

    private Task<bool> IsActiveOwner(long userId, long communityId)
    {
        return _communityAccessService.HasCommunityRole(
            userId,
            communityId,
            new[] { CommunityUserRole.Owner });
    }

    private static TeacherResponse Map(UserIdentityResponse user, CommunityUser membership)
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

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
