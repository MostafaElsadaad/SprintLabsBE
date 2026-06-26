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

namespace Application.Features.Communities.Teachers.ListTeachers;

public class ListTeachersQueryHandler : IRequestHandler<ListTeachersQuery, List<TeacherResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;

    public ListTeachersQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<CommunityUser> communityUserRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _communityUserRepository = communityUserRepository;
    }

    public async Task<List<TeacherResponse>> Handle(
        ListTeachersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0)
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

        var memberships = await _communityUserRepository.AsQueryable()
            .Where(x =>
                x.CommunityId == request.CommunityId
                && x.Role == CommunityUserRole.Teacher)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<TeacherResponse>();
        foreach (var membership in memberships)
        {
            var teacher = await _userService.GetCurrentUser(membership.UserId);
            if (teacher == null)
            {
                continue;
            }

            result.Add(new TeacherResponse
            {
                UserId = teacher.Id,
                Name = teacher.Name,
                Email = teacher.Email,
                Status = membership.Status.ToString(),
                CreatedAt = membership.CreatedAt
            });
        }

        return result;
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
