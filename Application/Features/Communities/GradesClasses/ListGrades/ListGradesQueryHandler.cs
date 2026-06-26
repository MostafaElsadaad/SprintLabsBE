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
    private readonly IBaseRepository<Class> _classRepository;

    public ListGradesQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<Class> classRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
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

        var classCounts = await _classRepository.AsQueryable()
            .Where(x =>
                x.CommunityId == request.CommunityId
                && x.Status == ClassStatus.Active)
            .GroupBy(x => x.GradeId)
            .Select(x => new { GradeId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.GradeId, x => x.Count, cancellationToken);

        var grades = await _gradeRepository.AsQueryable()
            .Where(x => x.CommunityId == request.CommunityId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return grades
            .Select(x => new GradeResponse
            {
                Id = x.Id,
                CommunityId = x.CommunityId,
                Name = x.Name,
                SortOrder = x.SortOrder,
                ClassCount = classCounts.TryGetValue(x.Id, out var count) ? count : 0,
                CreatedAt = x.CreatedAt
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
}
