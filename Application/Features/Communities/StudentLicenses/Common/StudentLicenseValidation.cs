using System.Net;
using System.Net.Mail;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.StudentLicenses.Common;

public static class StudentLicenseValidation
{
    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static async Task EnsureValidGradeClass(
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<Class> classRepository,
        long communityId,
        long gradeId,
        long classId,
        CancellationToken cancellationToken)
    {
        var gradeExists = await gradeRepository.AsQueryable()
            .AnyAsync(
                x => x.Id == gradeId && x.CommunityId == communityId,
                cancellationToken);
        if (!gradeExists)
        {
            throw NotFound();
        }

        var classIsValid = await classRepository.AsQueryable()
            .AnyAsync(
                x => x.Id == classId
                     && x.CommunityId == communityId
                     && x.GradeId == gradeId
                     && x.Status == ClassStatus.Active,
                cancellationToken);
        if (!classIsValid)
        {
            throw NotFound();
        }
    }

    public static async Task EnsureNoDuplicateNonRevokedEmail(
        IBaseRepository<StudentLicense> studentLicenseRepository,
        long communityId,
        string email,
        long? excludedLicenseId,
        CancellationToken cancellationToken)
    {
        var duplicate = await studentLicenseRepository.AsQueryable()
            .AnyAsync(
                x => x.CommunityId == communityId
                     && x.Email == email
                     && x.Status != StudentLicenseStatus.Revoked
                     && (!excludedLicenseId.HasValue || x.Id != excludedLicenseId.Value),
                cancellationToken);
        if (duplicate)
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
