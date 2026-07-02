using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

using ClassEntity = Domain.Models.Class;

namespace Application.Features.Communities.Students.Common;

public static class CommunityStudentValidation
{
    public static async Task EnsureValidGradeClassFilters(
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<ClassEntity> classRepository,
        long communityId,
        long? gradeId,
        long? classId,
        CancellationToken cancellationToken)
    {
        if (gradeId.HasValue)
        {
            var gradeExists = await gradeRepository.AsQueryable()
                .AnyAsync(
                    x => x.Id == gradeId.Value && x.CommunityId == communityId,
                    cancellationToken);
            if (!gradeExists)
            {
                throw NotFound();
            }
        }

        if (classId.HasValue)
        {
            var classQuery = classRepository.AsQueryable()
                .Where(x =>
                    x.Id == classId.Value
                    && x.CommunityId == communityId
                    && x.Status == ClassStatus.Active);

            if (gradeId.HasValue)
            {
                classQuery = classQuery.Where(x => x.GradeId == gradeId.Value);
            }

            var classExists = await classQuery.AnyAsync(cancellationToken);
            if (!classExists)
            {
                throw NotFound();
            }
        }
    }

    public static void EnsureValidPaging(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0 || pageSize <= 0)
        {
            throw InvalidInput();
        }
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
