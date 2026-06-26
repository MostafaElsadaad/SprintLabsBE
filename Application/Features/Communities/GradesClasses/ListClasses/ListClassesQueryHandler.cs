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

namespace Application.Features.Communities.GradesClasses.ListClasses;

public class ListClassesQueryHandler : IRequestHandler<ListClassesQuery, List<ClassResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<Class> _classRepository;

    public ListClassesQueryHandler(
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

    public async Task<List<ClassResponse>> Handle(
        ListClassesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            request.GradeId <= 0)
        {
            throw InvalidInput();
        }

        await CommunityGradesClassesAuthorization.EnsureCanManage(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        if (request.GradeId.HasValue)
        {
            var gradeExists = await _gradeRepository.AsQueryable()
                .AnyAsync(
                    x => x.Id == request.GradeId.Value && x.CommunityId == request.CommunityId,
                    cancellationToken);
            if (!gradeExists)
            {
                throw NotFound();
            }
        }

        var query = _classRepository.AsQueryable()
            .Where(x =>
                x.CommunityId == request.CommunityId
                && x.Status == ClassStatus.Active);

        if (request.GradeId.HasValue)
        {
            query = query.Where(x => x.GradeId == request.GradeId.Value);
        }

        var classes = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return classes.Select(Map).ToList();
    }

    private static ClassResponse Map(Class classEntity)
    {
        return new ClassResponse
        {
            Id = classEntity.Id,
            CommunityId = classEntity.CommunityId,
            GradeId = classEntity.GradeId,
            Name = classEntity.Name,
            Status = classEntity.Status.ToString(),
            CreatedAt = classEntity.CreatedAt
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
