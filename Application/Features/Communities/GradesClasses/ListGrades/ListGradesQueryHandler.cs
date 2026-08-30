using System.Net;

using Application.Features.Communities.GradesClasses.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.GradesClasses.ListGrades;

public class ListGradesQueryHandler : IRequestHandler<ListGradesQuery, List<GradeResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Grade> _gradeRepository;

    public ListGradesQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<Grade> gradeRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _gradeRepository = gradeRepository;
    }

    public async Task<List<GradeResponse>> Handle(
        ListGradesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0)
        {
            throw InvalidInput();
        }

        await CommunityGradesClassesAuthorization.EnsureCanManage(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        var grades = await _gradeRepository.AsQueryable()
            .Where(x => x.CommunityId == request.CommunityId && x.Value.HasValue)
            .OrderBy(x => x.Value)
            .ToListAsync(cancellationToken);

        var values = grades.Select(x => x.Value!.Value).ToList();
        if (values.Count != 6 || !values.SequenceEqual(new[] { 7, 8, 9, 10, 11, 12 }))
        {
            throw InvalidGradeContext();
        }

        return grades
            .Select(x => new GradeResponse
            {
                Id = x.Id,
                Value = x.Value!.Value
            })
            .ToList();
    }

    private static GenericException InvalidInput()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.BadRequest,
            errorCode: ErrorCode.Failure);
    }

    private static GenericException InvalidGradeContext()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.Conflict,
            errorCode: ErrorCode.Failure);
    }
}
