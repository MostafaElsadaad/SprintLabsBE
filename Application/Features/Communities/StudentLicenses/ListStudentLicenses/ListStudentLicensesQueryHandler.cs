using System.Net;

using Application.Features.Communities.StudentLicenses.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.StudentLicenses.ListStudentLicenses;

public class ListStudentLicensesQueryHandler
    : IRequestHandler<ListStudentLicensesQuery, List<StudentLicenseResponse>>
{
    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<Class> _classRepository;

    public ListStudentLicensesQueryHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<Class> classRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _studentLicenseRepository = studentLicenseRepository;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
    }

    public async Task<List<StudentLicenseResponse>> Handle(
        ListStudentLicensesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
            request.GradeId <= 0 ||
            request.ClassId <= 0)
        {
            throw InvalidInput();
        }

        await StudentLicenseAuthorization.EnsureActiveOwner(
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

        if (request.ClassId.HasValue)
        {
            var classExists = await _classRepository.AsQueryable()
                .AnyAsync(
                    x => x.Id == request.ClassId.Value && x.CommunityId == request.CommunityId,
                    cancellationToken);
            if (!classExists)
            {
                throw NotFound();
            }
        }

        var query = _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .Where(x => x.CommunityId == request.CommunityId);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.GradeId.HasValue)
        {
            query = query.Where(x => x.GradeId == request.GradeId.Value);
        }

        if (request.ClassId.HasValue)
        {
            query = query.Where(x => x.ClassId == request.ClassId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = StudentLicenseValidation.NormalizeEmail(request.Search);
            query = query.Where(x => x.Email.Contains(search));
        }

        var licenses = await query
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<StudentLicenseResponse>();
        foreach (var license in licenses)
        {
            result.Add(await StudentLicenseMapper.Map(license, _userService));
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

    private static GenericException NotFound()
    {
        return new GenericException(
            message: ErrorMessage.NotFound,
            statusCode: HttpStatusCode.NotFound,
            errorCode: ErrorCode.Failure);
    }
}
