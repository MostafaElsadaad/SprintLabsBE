using System.Net;

using Application.Features.Communities.Teachers.Common;
using Domain.Enums;
using Domain.Services;
using MediatR;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.Teachers.ReplaceTeacherClassAssignments;

public class ReplaceTeacherClassAssignmentsCommandHandler : IRequestHandler<ReplaceTeacherClassAssignmentsCommand, TeacherResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly ITeacherClassAssignmentService _assignmentService;

    public ReplaceTeacherClassAssignmentsCommandHandler(IUserService userService, ICommunityAccessService communityAccessService, ITeacherClassAssignmentService assignmentService)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _assignmentService = assignmentService;
    }

    public async Task<TeacherResponse> Handle(ReplaceTeacherClassAssignmentsCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0 || request.TeacherUserId <= 0 || request.ClassIds.Any(x => x <= 0)) throw InvalidInput();
        var caller = await _userService.GetCurrentUser(request.UserId);
        if (caller == null) throw NotFound();
        if (caller.IsSuspended || !await _communityAccessService.HasCommunityRole(request.UserId, request.CommunityId, new[] { CommunityUserRole.Owner })) throw Forbidden();
        var teacher = await _userService.GetCurrentUser(request.TeacherUserId);
        if (teacher == null) throw NotFound();

        var assignments = await _assignmentService.ReplaceAsync(request.CommunityId, request.TeacherUserId, request.ClassIds.Distinct().ToList(), cancellationToken);
        return new TeacherResponse
        {
            UserId = teacher.Id, Name = teacher.Name, Email = teacher.Email, Status = CommunityUserStatus.Active.ToString(), CreatedAt = assignments.FirstOrDefault()?.CreatedAt ?? DateTime.UtcNow,
            Classes = assignments.OrderBy(x => x.Class.Grade.Value).ThenBy(x => x.Class.Name)
                .Select(x => new TeacherClassResponse { ClassId = x.ClassId, ClassName = x.Class.Name, GradeId = x.Class.GradeId, Grade = x.Class.Grade.Value!.Value }).ToList()
        };
    }

    private static GenericException InvalidInput() => new(ErrorCode.Failure, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
    private static GenericException Forbidden() => new(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
    private static GenericException NotFound() => new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
}
