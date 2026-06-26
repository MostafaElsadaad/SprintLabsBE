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

namespace Application.Features.Communities.GradesClasses.CreateClass;

public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, ClassResponse>
{
    private const int NameMaxLength = 120;

    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<Class> _classRepository;

    public CreateClassCommandHandler(
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

    public async Task<ClassResponse> Handle(
        CreateClassCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            request.GradeId <= 0 ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            throw InvalidInput();
        }

        var name = request.Name.Trim();
        if (name.Length > NameMaxLength)
        {
            throw InvalidInput();
        }

        await CommunityGradesClassesAuthorization.EnsureCanManage(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        var gradeExists = await _gradeRepository.AsQueryable()
            .AnyAsync(
                x => x.Id == request.GradeId && x.CommunityId == request.CommunityId,
                cancellationToken);
        if (!gradeExists)
        {
            throw NotFound();
        }

        var classEntity = new Class
        {
            CommunityId = request.CommunityId,
            GradeId = request.GradeId,
            Name = name,
            Status = ClassStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _classRepository.AddAsync(classEntity);
        await _classRepository.SaveChangesAsync();

        return Map(classEntity);
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
