using System.Net;
using System.Globalization;

using Application.Features.Communities.Students.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Communities.Students.GetStudentDetail;

public class GetStudentDetailQueryHandler : IRequestHandler<GetStudentDetailQuery, CommunityStudentDetailResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<Player> _playerRepository;

    public GetStudentDetailQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<Player> playerRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _studentLicenseRepository = studentLicenseRepository;
        _playerRepository = playerRepository;
    }

    public async Task<CommunityStudentDetailResponse> Handle(
        GetStudentDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0 || request.PlayerProfileId <= 0)
        {
            throw InvalidInput();
        }

        await CommunityStudentAuthorization.EnsureCanViewStudents(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        var license = await _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(
                x => x.CommunityId == request.CommunityId
                     && x.PlayerProfileId == request.PlayerProfileId
                     && x.Status != StudentLicenseStatus.Revoked,
                cancellationToken);
        if (license == null)
        {
            throw NotFound();
        }

        var player = await _playerRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.PlayerProfileId, cancellationToken);
        if (player == null)
        {
            throw NotFound();
        }

        UserIdentityResponse? user = null;
        if (license.UserId.HasValue)
        {
            user = await _userService.GetCurrentUser(license.UserId.Value);
        }

        return new CommunityStudentDetailResponse
        {
            UserId = license.UserId ?? 0,
            PlayerProfileId = player.Id,
            Name = player.Name,
            Email = player.Email,
            AvatarUrl = player.AvatarUrl ?? user?.AvatarUrl,
            Gold = player.Gold,
            Experience = player.Experience,
            Level = player.Level,
            LicenseStatus = license.Status.ToString(),
            GradeId = license.GradeId,
            GradeName = license.Grade?.Value?.ToString(CultureInfo.InvariantCulture) ?? license.Grade?.Name ?? string.Empty,
            ClassId = license.ClassId,
            ClassName = license.Class?.Name ?? string.Empty,
            ActivatedAt = license.ActivatedAt,
            CreatedAt = license.CreatedAt,
            Analytics = new CommunityStudentAnalyticsResponse()
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
