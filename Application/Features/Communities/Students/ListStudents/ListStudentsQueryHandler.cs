using System.Net;

using Application.Features.Communities.Students.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

using ClassEntity = Domain.Models.Class;

namespace Application.Features.Communities.Students.ListStudents;

public class ListStudentsQueryHandler
    : IRequestHandler<ListStudentsQuery, PagedResponse<CommunityStudentListItemResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<ClassEntity> _classRepository;
    private readonly IBaseRepository<Player> _playerRepository;

    public ListStudentsQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<ClassEntity> classRepository,
        IBaseRepository<Player> playerRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _studentLicenseRepository = studentLicenseRepository;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
        _playerRepository = playerRepository;
    }

    public async Task<PagedResponse<CommunityStudentListItemResponse>> Handle(
        ListStudentsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0)
        {
            throw InvalidInput();
        }

        CommunityStudentValidation.EnsureValidPaging(request.PageNumber, request.PageSize);

        await CommunityStudentAuthorization.EnsureCanViewStudents(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        await CommunityStudentValidation.EnsureValidGradeClassFilters(
            _gradeRepository,
            _classRepository,
            request.CommunityId,
            request.GradeId,
            request.ClassId,
            cancellationToken);

        var query = _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .Where(x => x.CommunityId == request.CommunityId);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.GradeId.HasValue)
        {
            query = query.Where(x => x.GradeId == request.GradeId.Value);
        }

        if (request.ClassId.HasValue)
        {
            query = query.Where(x => x.ClassId == request.ClassId.Value);
        }

        var search = NormalizeSearch(request.Search);
        List<long> matchingUserIds = new();
        if (!string.IsNullOrWhiteSpace(search))
        {
            matchingUserIds = await _userService.SearchUserIds(search, cancellationToken);
            var players = _playerRepository.AsQueryable();

            query = query.Where(x =>
                x.Email.ToLower().Contains(search)
                || (x.UserId.HasValue && matchingUserIds.Contains(x.UserId.Value))
                || (x.PlayerProfileId.HasValue
                    && players.Any(p =>
                        p.Id == x.PlayerProfileId.Value
                        && (p.Email.ToLower().Contains(search)
                            || p.Name.ToLower().Contains(search)))));
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var licenses = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(request.PageSize * (request.PageNumber - 1))
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var playerIds = licenses
            .Where(x => x.PlayerProfileId.HasValue)
            .Select(x => x.PlayerProfileId!.Value)
            .Distinct()
            .ToList();
        var playersById = await _playerRepository.AsQueryable()
            .Where(x => playerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var userIds = licenses
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .Distinct()
            .ToList();
        var usersById = (await _userService.GetUsersByIds(userIds, cancellationToken))
            .ToDictionary(x => x.Id);

        var items = licenses.Select(x =>
        {
            Player? player = null;
            if (x.PlayerProfileId.HasValue)
            {
                playersById.TryGetValue(x.PlayerProfileId.Value, out player);
            }

            UserIdentityResponse? user = null;
            if (x.UserId.HasValue)
            {
                usersById.TryGetValue(x.UserId.Value, out user);
            }

            return new CommunityStudentListItemResponse
            {
                LicenseId = x.Id,
                Email = x.Email,
                Status = x.Status.ToString(),
                UserId = x.UserId,
                PlayerProfileId = x.PlayerProfileId,
                PlayerName = player?.Name ?? user?.Name,
                AvatarUrl = player?.AvatarUrl ?? user?.AvatarUrl,
                GradeId = x.GradeId,
                GradeName = x.Grade?.Name ?? string.Empty,
                ClassId = x.ClassId,
                ClassName = x.Class?.Name ?? string.Empty,
                ActivatedAt = x.ActivatedAt,
                CreatedAt = x.CreatedAt
            };
        }).ToList();

        return new PagedResponse<CommunityStudentListItemResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }

    private static string? NormalizeSearch(string? search)
    {
        return string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim().ToLowerInvariant();
    }

    private static GenericException InvalidInput()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.BadRequest,
            errorCode: ErrorCode.Failure);
    }
}
