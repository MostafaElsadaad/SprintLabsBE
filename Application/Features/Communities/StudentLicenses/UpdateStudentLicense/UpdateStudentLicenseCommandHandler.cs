using System.Net;

using Application.Features.Communities.StudentLicenses.Common;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.StudentLicenses.UpdateStudentLicense;

public class UpdateStudentLicenseCommandHandler
    : IRequestHandler<UpdateStudentLicenseCommand, StudentLicenseResponse>
{
    private const int EmailMaxLength = 256;

    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<CommunityLicense> _communityLicenseRepository;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<Class> _classRepository;

    public UpdateStudentLicenseCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository,
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<Class> classRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _studentLicenseRepository = studentLicenseRepository;
        _communityLicenseRepository = communityLicenseRepository;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
    }

    public async Task<StudentLicenseResponse> Handle(
        UpdateStudentLicenseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            request.LicenseId <= 0 ||
            request.GradeId <= 0 ||
            request.ClassId <= 0 ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            throw InvalidInput();
        }

        var email = StudentLicenseValidation.NormalizeEmail(request.Email);
        if (email.Length > EmailMaxLength || !StudentLicenseValidation.IsValidEmail(email))
        {
            throw InvalidInput();
        }

        await StudentLicenseAuthorization.EnsureActiveOwner(
            _userService,
            _communityAccessService,
            request.UserId,
            request.CommunityId);

        await StudentLicenseValidation.EnsureValidGradeClass(
            _gradeRepository,
            _classRepository,
            request.CommunityId,
            request.GradeId,
            request.ClassId,
            cancellationToken);

        var studentLicense = await _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(
                x => x.Id == request.LicenseId && x.CommunityId == request.CommunityId,
                cancellationToken);
        if (studentLicense == null)
        {
            throw NotFound();
        }

        if (studentLicense.Email != email)
        {
            if (studentLicense.Status != StudentLicenseStatus.Pending)
            {
                throw InvalidInput();
            }

            var communityLicense = await _communityLicenseRepository.AsQueryable()
                .FirstOrDefaultAsync(x => x.CommunityId == request.CommunityId, cancellationToken);
            if (communityLicense == null)
            {
                throw NotFound();
            }

            if (studentLicense.EmailChangeCount >= communityLicense.StudentEmailChangeLimit)
            {
                throw InvalidInput();
            }

            await StudentLicenseValidation.EnsureNoDuplicateNonRevokedEmail(
                _studentLicenseRepository,
                request.CommunityId,
                email,
                studentLicense.Id,
                cancellationToken);

            studentLicense.Email = email;
            studentLicense.EmailChangeCount++;
        }

        studentLicense.GradeId = request.GradeId;
        studentLicense.ClassId = request.ClassId;
        studentLicense.UpdatedAt = DateTime.UtcNow;

        await _studentLicenseRepository.UpdateAsync(studentLicense);
        await _studentLicenseRepository.SaveChangesAsync();

        studentLicense = await _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .FirstAsync(x => x.Id == studentLicense.Id, cancellationToken);

        return await StudentLicenseMapper.Map(studentLicense, _userService);
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
