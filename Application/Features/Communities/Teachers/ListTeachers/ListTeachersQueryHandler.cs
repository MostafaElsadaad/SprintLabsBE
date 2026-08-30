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
using Shared.Responses;

namespace Application.Features.Communities.Teachers.ListTeachers;

public class ListTeachersQueryHandler : IRequestHandler<ListTeachersQuery, PagedResponse<TeacherResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<Class> _classRepository;
    private readonly IBaseRepository<TeacherClassAssignment> _assignmentRepository;

    public ListTeachersQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<CommunityUser> communityUserRepository,
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<Class> classRepository,
        IBaseRepository<TeacherClassAssignment> assignmentRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityUserRepository = communityUserRepository;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
        _assignmentRepository = assignmentRepository;
    }

    public async Task<PagedResponse<TeacherResponse>> Handle(
        ListTeachersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0)
        {
            throw InvalidInput();
        }
        if (request.PageNumber <= 0 || request.PageSize <= 0 || request.PageSize > 100)
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

        if (request.GradeId.HasValue)
        {
            var gradeValid = await _gradeRepository.AsQueryable().AnyAsync(x => x.Id == request.GradeId && x.CommunityId == request.CommunityId && x.Value.HasValue && x.Value >= 7 && x.Value <= 12, cancellationToken);
            if (!gradeValid) throw NotFound();
        }
        if (request.ClassId.HasValue)
        {
            var classQuery = _classRepository.AsQueryable().Where(x => x.Id == request.ClassId && x.CommunityId == request.CommunityId && x.Status == ClassStatus.Active && x.Grade.Value.HasValue && x.Grade.Value >= 7 && x.Grade.Value <= 12);
            if (request.GradeId.HasValue) classQuery = classQuery.Where(x => x.GradeId == request.GradeId.Value);
            if (!await classQuery.AnyAsync(cancellationToken)) throw NotFound();
        }

        var memberships = _communityUserRepository.AsQueryable()
            .Where(x =>
                x.CommunityId == request.CommunityId
                && x.Role == CommunityUserRole.Teacher)
            .AsQueryable();
        if (request.Status.HasValue) memberships = memberships.Where(x => x.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var ids = await _userService.SearchUserIds(request.Search.Trim(), cancellationToken);
            memberships = memberships.Where(x => ids.Contains(x.UserId));
        }
        if (request.ClassId.HasValue) memberships = memberships.Where(x => _assignmentRepository.AsQueryable().Any(a => a.TeacherUserId == x.UserId && a.ClassId == request.ClassId.Value));
        if (request.GradeId.HasValue) memberships = memberships.Where(x => _assignmentRepository.AsQueryable().Any(a => a.TeacherUserId == x.UserId && a.Class.CommunityId == request.CommunityId && a.Class.Status == ClassStatus.Active && a.Class.GradeId == request.GradeId.Value));

        var total = await memberships.CountAsync(cancellationToken);
        var page = await memberships.OrderBy(x => x.CreatedAt).ThenBy(x => x.UserId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        var teachers = (await _userService.GetUsersByIds(page.Select(x => x.UserId), cancellationToken)).ToDictionary(x => x.Id);
        var teacherIds = page.Select(x => x.UserId).ToList();
        var assignments = await _assignmentRepository.AsQueryable().Include(x => x.Class).ThenInclude(x => x.Grade)
            .Where(x => teacherIds.Contains(x.TeacherUserId) && x.Class.CommunityId == request.CommunityId && x.Class.Status == ClassStatus.Active && x.Class.Grade.Value.HasValue && x.Class.Grade.Value >= 7 && x.Class.Grade.Value <= 12)
            .ToListAsync(cancellationToken);
        var classesByTeacher = assignments.GroupBy(x => x.TeacherUserId).ToDictionary(x => x.Key, x => x.OrderBy(a => a.Class.Grade.Value).ThenBy(a => a.Class.Name).Select(a => new TeacherClassResponse { ClassId = a.ClassId, ClassName = a.Class.Name, GradeId = a.Class.GradeId, Grade = a.Class.Grade.Value!.Value }).ToList());

        var result = new List<TeacherResponse>();
        foreach (var membership in page)
        {
            if (!teachers.TryGetValue(membership.UserId, out var teacher)) continue;

            result.Add(new TeacherResponse
            {
                UserId = teacher.Id,
                Name = teacher.Name,
                Email = teacher.Email,
                Status = membership.Status.ToString(),
                CreatedAt = membership.CreatedAt,
                Classes = classesByTeacher.GetValueOrDefault(membership.UserId, new List<TeacherClassResponse>())
            });
        }

        return new PagedResponse<TeacherResponse>(result, request.PageNumber, request.PageSize, total);
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
