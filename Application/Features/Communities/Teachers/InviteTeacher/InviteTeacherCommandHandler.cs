using System.Net;

using Application.Features.Accounts.TeacherAuthentication.Common;

using Application.Features.Communities.Teachers.Common;

using Domain.Enums;
using Domain.Services;

using MediatR;

using Microsoft.Extensions.Options;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Communities.Teachers.InviteTeacher;

public class InviteTeacherCommandHandler : IRequestHandler<InviteTeacherCommand, TeacherResponse>
{
    private readonly ICommunityAccessService _communityAccessService;
    private readonly ITeacherInvitationService _teacherInvitationService;
    private readonly IEmailService _emailService;
    private readonly Shared.Options.FrontendOptions _frontendOptions;

    public InviteTeacherCommandHandler(
        ICommunityAccessService communityAccessService,
        ITeacherInvitationService teacherInvitationService,
        IEmailService emailService,
        IOptions<Shared.Options.FrontendOptions> frontendOptions)
    {
        _communityAccessService = communityAccessService;
        _teacherInvitationService = teacherInvitationService;
        _emailService = emailService;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task<TeacherResponse> Handle(
        InviteTeacherCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            string.IsNullOrWhiteSpace(request.Email) ||
            request.ClassIds == null ||
            request.ClassIds.Any(x => x <= 0))
        {
            throw InvalidInput();
        }

        var email = NormalizeEmail(request.Email);
        if (!await _communityAccessService.HasCommunityRole(
                request.UserId,
                request.CommunityId,
                new[] { CommunityUserRole.Owner }))
        {
            throw Forbidden();
        }

        var issue = await _teacherInvitationService.IssueAsync(
            request.UserId,
            request.CommunityId,
            email,
            request.ClassIds.Distinct().ToList(),
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(issue.InvitationToken))
        {
            await _emailService.SendCommunityInvitationEmailAsync(issue.Email, issue.Name, issue.CommunityName,
                TeacherAuthenticationLinkBuilder.Invitation(_frontendOptions, issue.InvitationToken), cancellationToken);
        }
        return new TeacherResponse
        {
            UserId = issue.UserId,
            Name = issue.Name,
            Email = issue.Email,
            Status = issue.Status,
            CreatedAt = DateTime.UtcNow,
            Classes = issue.Classes
                .Select(x => new TeacherClassResponse
                {
                    ClassId = x.ClassId,
                    ClassName = x.ClassName,
                    GradeId = x.GradeId,
                    Grade = x.Grade
                })
                .ToList()
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
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

}
