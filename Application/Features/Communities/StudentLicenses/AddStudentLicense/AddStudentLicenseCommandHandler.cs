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
using Shared.Responses;

namespace Application.Features.Communities.StudentLicenses.AddStudentLicense;

public class AddStudentLicenseCommandHandler
    : IRequestHandler<AddStudentLicenseCommand, StudentLicenseResponse>
{
    private const int EmailMaxLength = 256;

    private readonly IUserService _userService;
    private readonly ICommunityAccessService _communityAccessService;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<CommunityLicense> _communityLicenseRepository;
    private readonly IBaseRepository<Grade> _gradeRepository;
    private readonly IBaseRepository<Class> _classRepository;
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly IPlayerRepository _playerRepository;

    public AddStudentLicenseCommandHandler(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<CommunityLicense> communityLicenseRepository,
        IBaseRepository<Grade> gradeRepository,
        IBaseRepository<Class> classRepository,
        IBaseRepository<CommunityUser> communityUserRepository,
        IPlayerRepository playerRepository)
    {
        _userService = userService;
        _communityAccessService = communityAccessService;
        _studentLicenseRepository = studentLicenseRepository;
        _communityLicenseRepository = communityLicenseRepository;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
        _communityUserRepository = communityUserRepository;
        _playerRepository = playerRepository;
    }

    public async Task<StudentLicenseResponse> Handle(
        AddStudentLicenseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 ||
            request.CommunityId <= 0 ||
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

        await StudentLicenseValidation.EnsureNoDuplicateNonRevokedEmail(
            _studentLicenseRepository,
            request.CommunityId,
            email,
            excludedLicenseId: null,
            cancellationToken);

        var communityLicense = await _communityLicenseRepository.AsQueryable()
            .FirstOrDefaultAsync(x => x.CommunityId == request.CommunityId, cancellationToken);
        if (communityLicense == null || communityLicense.UsedStudents >= communityLicense.MaxStudents)
        {
            throw InvalidInput();
        }

        var studentUser = await _userService.FindByEmail(email);
        var shouldActivateImmediately = !string.IsNullOrWhiteSpace(studentUser?.GoogleId);
        Player? playerProfile = null;
        CommunityUser? existingMembership = null;
        var now = DateTime.UtcNow;

        if (shouldActivateImmediately)
        {
            existingMembership = await GetExistingCommunityMembership(
                request.CommunityId,
                studentUser!.Id,
                cancellationToken);

            if (existingMembership != null && existingMembership.Role != CommunityUserRole.Student)
            {
                throw InvalidInput();
            }

            playerProfile = await FindOrCreatePlayerProfile(studentUser);
            await CreateOrRestoreStudentMembership(
                request.CommunityId,
                studentUser.Id,
                existingMembership,
                now);
        }

        var studentLicense = new StudentLicense
        {
            CommunityId = request.CommunityId,
            Email = email,
            UserId = shouldActivateImmediately ? studentUser?.Id : null,
            PlayerProfileId = playerProfile?.Id,
            GradeId = request.GradeId,
            ClassId = request.ClassId,
            Status = shouldActivateImmediately ? StudentLicenseStatus.Active : StudentLicenseStatus.Pending,
            EmailChangeCount = 0,
            AssignedByUserId = request.UserId,
            ActivatedAt = shouldActivateImmediately ? now : null,
            CreatedAt = now
        };

        communityLicense.UsedStudents++;
        communityLicense.UpdatedAt = now;

        await _studentLicenseRepository.AddAsync(studentLicense);
        await _communityLicenseRepository.UpdateAsync(communityLicense);
        await _studentLicenseRepository.SaveChangesAsync();

        studentLicense = await _studentLicenseRepository.AsQueryable()
            .Include(x => x.Grade)
            .Include(x => x.Class)
            .FirstAsync(x => x.Id == studentLicense.Id, cancellationToken);

        return await StudentLicenseMapper.Map(studentLicense, _userService);
    }

    private async Task<CommunityUser?> GetExistingCommunityMembership(
        long communityId,
        long userId,
        CancellationToken cancellationToken)
    {
        return await _communityUserRepository.AsQueryable()
            .FirstOrDefaultAsync(
                x => x.CommunityId == communityId && x.UserId == userId,
                cancellationToken);
    }

    private async Task<Player> FindOrCreatePlayerProfile(UserIdentityResponse user)
    {
        var existingByUser = await _playerRepository.GetByUserIdAsync(user.Id);
        if (existingByUser != null)
        {
            return existingByUser;
        }

        if (string.IsNullOrWhiteSpace(user.GoogleId))
        {
            throw InvalidInput();
        }

        var existingByGoogleId = await _playerRepository.GetByGoogleIdAsync(user.GoogleId);
        if (existingByGoogleId != null)
        {
            if (existingByGoogleId.UserId.HasValue && existingByGoogleId.UserId.Value != user.Id)
            {
                throw new GenericException(
                    message: ErrorMessage.ExistingRecord,
                    statusCode: HttpStatusCode.Conflict,
                    errorCode: ErrorCode.Failure);
            }

            existingByGoogleId.UserId = user.Id;
            existingByGoogleId.Email = user.Email;
            existingByGoogleId.Name = user.Name;
            existingByGoogleId.AvatarUrl = user.AvatarUrl;
            return await _playerRepository.UpdatePlayer(existingByGoogleId);
        }

        return await _playerRepository.CreateAsync(new Player
        {
            UserId = user.Id,
            GoogleId = user.GoogleId,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl
        });
    }

    private async Task CreateOrRestoreStudentMembership(
        long communityId,
        long userId,
        CommunityUser? existingMembership,
        DateTime now)
    {
        if (existingMembership == null)
        {
            await _communityUserRepository.AddAsync(new CommunityUser
            {
                CommunityId = communityId,
                UserId = userId,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Active,
                CreatedAt = now
            });
            return;
        }

        if (existingMembership.Status == CommunityUserStatus.Active)
        {
            return;
        }

        existingMembership.Status = CommunityUserStatus.Active;
        existingMembership.UpdatedAt = now;
        await _communityUserRepository.UpdateAsync(existingMembership);
    }

    private static GenericException InvalidInput()
    {
        return new GenericException(
            message: ErrorMessage.InvalidInput,
            statusCode: HttpStatusCode.BadRequest,
            errorCode: ErrorCode.Failure);
    }
}
