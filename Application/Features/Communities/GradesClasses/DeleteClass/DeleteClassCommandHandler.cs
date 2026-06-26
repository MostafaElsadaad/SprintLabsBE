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

namespace Application.Features.Communities.GradesClasses.DeleteClass;

public class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand, ClassResponse>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<Class> _classRepository;

    public DeleteClassCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<Class> classRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _classRepository = classRepository;
    }

    public async Task<ClassResponse> Handle(
        DeleteClassCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.CommunityId <= 0 || request.ClassId <= 0)
        {
            throw InvalidInput();
        }

        await CommunityGradesClassesAuthorization.EnsureCanManage(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        var classEntity = await _classRepository.AsQueryable()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClassId && x.CommunityId == request.CommunityId,
                cancellationToken);
        if (classEntity == null)
        {
            throw NotFound();
        }

        if (classEntity.Status != ClassStatus.Deleted)
        {
            classEntity.Status = ClassStatus.Deleted;
            classEntity.UpdatedAt = DateTime.UtcNow;
            await _classRepository.UpdateAsync(classEntity);
            await _classRepository.SaveChangesAsync();
        }

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
