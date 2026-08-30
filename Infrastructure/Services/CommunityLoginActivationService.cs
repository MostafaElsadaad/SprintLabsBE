using System.Data;
using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Services.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Shared.Enums;
using Shared.Exceptions;

namespace Infrastructure.Services;

public class CommunityLoginActivationService : ICommunityLoginActivationService
{
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;
    private readonly IBaseRepository<StudentLicense> _studentLicenseRepository;
    private readonly IBaseRepository<TeacherInvitation>? _teacherInvitationRepository;
    private readonly ApplicationDbContext? _context;

    public CommunityLoginActivationService(
        IBaseRepository<CommunityUser> communityUserRepository,
        IBaseRepository<StudentLicense> studentLicenseRepository,
        IBaseRepository<TeacherInvitation>? teacherInvitationRepository = null,
        ApplicationDbContext? context = null)
    {
        _communityUserRepository = communityUserRepository;
        _studentLicenseRepository = studentLicenseRepository;
        _teacherInvitationRepository = teacherInvitationRepository;
        _context = context;
    }

    public async Task ActivateEligiblePendingTeacherMembershipsAsync(
        long userId,
        string verifiedEmail,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(verifiedEmail);
        if (_teacherInvitationRepository == null)
        {
            return;
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        if (_context != null)
        {
            await StaffCommunityMembershipIntegrity.LockUserAsync(_context, userId, cancellationToken);
        }

        var currentStaffMemberships = _context != null
            ? await StaffCommunityMembershipIntegrity.GetCurrentStaffMembershipsAsync(_context, userId, cancellationToken)
            : await _communityUserRepository.AsQueryable()
                .Where(x => x.UserId == userId &&
                            (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                            (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active))
                .ToListAsync(cancellationToken);
        if (currentStaffMemberships.Select(x => x.CommunityId).Distinct().Count() > 1)
        {
            throw TeacherCommunityConflict();
        }

        var memberships = await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == userId
                && x.Role == CommunityUserRole.Teacher
                && x.Status == CommunityUserStatus.Pending)
            .ToListAsync(cancellationToken);
        if (memberships.Count == 0)
        {
            return;
        }

        var membershipIds = memberships.Select(x => x.Id).ToList();
        var invitations = await _teacherInvitationRepository.AsQueryable()
            .Where(x => membershipIds.Contains(x.CommunityUserId))
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var membership in memberships)
        {
            var membershipInvitations = invitations.Where(x => x.CommunityUserId == membership.Id).ToList();
            var activeInvitation = membershipInvitations
                .Where(x => x.RevokedAt == null
                    && x.AcceptedAt == null
                    && x.ExpiresAt > now
                    && NormalizeEmail(x.InvitedEmail) == normalizedEmail)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (activeInvitation == null && membershipInvitations.Count != 0)
            {
                continue;
            }

            membership.Status = CommunityUserStatus.Active;
            membership.UpdatedAt = now;
            await _communityUserRepository.UpdateAsync(membership);
            if (activeInvitation != null)
            {
                activeInvitation.AcceptedAt = now;
                await _teacherInvitationRepository.UpdateAsync(activeInvitation);
            }

            changed = true;
        }

        if (changed)
        {
            await _communityUserRepository.SaveChangesAsync();
        }
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
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

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_context == null || !_context.Database.IsRelational())
        {
            return null;
        }

        return await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    private static GenericException TeacherCommunityConflict()
    {
        return new GenericException(
            ErrorCode.TeacherAlreadyBelongsToAnotherCommunity,
            ErrorMessage.TeacherAlreadyBelongsToAnotherCommunity,
            HttpStatusCode.Conflict);
    }
}
