using System.Net;
using System.Text;

using Application.Features.Accounts.TeacherAuthentication.Common;
using Application.Features.Admin.Communities.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.CreateCommunity;

public class CreateCommunityCommandHandler : IRequestHandler<CreateCommunityCommand, CommunityResponse>
{
    private static readonly int[] SupportedGradeValues = [7, 8, 9, 10, 11, 12];
    private readonly IUserService _userService;
    private readonly IBaseRepository<Community> _communityRepository;
    private readonly ITeacherInvitationService _invitationService;
    private readonly IEmailService _emailService;
    private readonly Shared.Options.FrontendOptions _frontendOptions;

    public CreateCommunityCommandHandler(
        IUserService userService,
        IBaseRepository<Community> communityRepository,
        ITeacherInvitationService invitationService,
        IEmailService emailService,
        Microsoft.Extensions.Options.IOptions<Shared.Options.FrontendOptions> frontendOptions)
    {
        _userService = userService;
        _communityRepository = communityRepository;
        _invitationService = invitationService;
        _emailService = emailService;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task<CommunityResponse> Handle(CreateCommunityCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(_userService, request.AuthenticatedUserId);

        var name = request.Name.Trim();
        var email = request.AdminEmail.Trim();
        var slug = CreateSlug(name);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(slug))
        {
            throw InvalidInput();
        }

        if (await _communityRepository.AsQueryable().AnyAsync(x => x.Slug == slug))
        {
            throw new GenericException(
                message: ErrorMessage.ExistingRecord,
                statusCode: HttpStatusCode.BadRequest,
                errorCode: ErrorCode.Failure);
        }

        var community = await _communityRepository.AddAsync(new Community
        {
            Name = name,
            Slug = slug,
            Status = CommunityStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Grades = SupportedGradeValues
                .Select(value => new Grade
                {
                    Value = value,
                    Name = $"Grade {value}",
                    SortOrder = value,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList()
        });
        await _communityRepository.SaveChangesAsync();

        var issue = await _invitationService.IssueCommunityAdminSetupAsync(
            request.AuthenticatedUserId,
            community.Id,
            email,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(issue.InvitationToken))
        {
            await _emailService.SendCommunityAdminSetupEmailAsync(
                issue.Email,
                issue.Name,
                issue.CommunityName,
                TeacherAuthenticationLinkBuilder.CommunityAdminSetup(_frontendOptions, issue.InvitationToken),
                cancellationToken);
        }

        return MapCommunity(community);
    }

    private static string CreateSlug(string name)
    {
        var builder = new StringBuilder(name.Length);
        var previousWasSeparator = false;
        foreach (var character in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static CommunityResponse MapCommunity(Community community)
    {
        return new CommunityResponse
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
}
