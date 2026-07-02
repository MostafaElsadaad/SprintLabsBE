using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CommunityLoginActivationService : ICommunityLoginActivationService
{
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;

    public CommunityLoginActivationService(
        IBaseRepository<CommunityUser> communityUserRepository,
        IBaseRepository<StudentLicense> studentLicenseRepository)
    {
        _communityUserRepository = communityUserRepository;
        _studentLicenseRepository = studentLicenseRepository;
    }

    public async Task ActivatePendingTeacherMembershipsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var pendingTeacherMemberships = await _communityUserRepository.AsQueryable()
            .Where(x =>
                x.UserId == userId
                && x.Role == CommunityUserRole.Teacher
                && x.Status == CommunityUserStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingTeacherMemberships.Count == 0)
        {
            return;
        }

        foreach (var membership in pendingTeacherMemberships)
        {
            membership.Status = CommunityUserStatus.Active;
            membership.UpdatedAt = DateTime.UtcNow;
            await _communityUserRepository.UpdateAsync(membership);
        }

        await _communityUserRepository.SaveChangesAsync();
    }

    public async Task ActivatePendingStudentLicensesAsync(
        long userId,
        long playerProfileId,
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var pendingLicenses = await _studentLicenseRepository.AsQueryable()
            .Where(x =>
                x.Email.ToLower() == normalizedEmail
                && x.Status == StudentLicenseStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingLicenses.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var studentLicense in pendingLicenses)
        {
            var hasStudentAccess = await CreateOrRestoreStudentMembership(
                studentLicense.CommunityId,
                userId,
                now,
                cancellationToken);

            if (!hasStudentAccess)
            {
                continue;
            }

            studentLicense.UserId = userId;
            studentLicense.PlayerProfileId = playerProfileId;
            studentLicense.Status = StudentLicenseStatus.Active;
            studentLicense.ActivatedAt = now;
            studentLicense.UpdatedAt = now;
            await _studentLicenseRepository.UpdateAsync(studentLicense);
        }

        await _studentLicenseRepository.SaveChangesAsync();
        await _communityUserRepository.SaveChangesAsync();
    }

    private async Task<bool> CreateOrRestoreStudentMembership(
        long communityId,
        long userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var membership = await _communityUserRepository.AsQueryable()
            .FirstOrDefaultAsync(
                x => x.CommunityId == communityId && x.UserId == userId,
                cancellationToken);

        if (membership == null)
        {
            await _communityUserRepository.AddAsync(new CommunityUser
            {
                CommunityId = communityId,
                UserId = userId,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Active,
                CreatedAt = now
            });

            return true;
        }

        if (membership.Role != CommunityUserRole.Student)
        {
            return false;
        }

        if (membership.Status == CommunityUserStatus.Active)
        {
            return true;
        }

        membership.Status = CommunityUserStatus.Active;
        membership.UpdatedAt = now;
        await _communityUserRepository.UpdateAsync(membership);
        return true;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
