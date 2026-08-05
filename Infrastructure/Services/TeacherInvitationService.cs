using System.Data;
using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Services.Common;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;
using Shared.Responses;

namespace Infrastructure.Services;

public class TeacherInvitationService : ITeacherInvitationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly TeacherAuthenticationOptions _options;

    public TeacherInvitationService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IRefreshTokenService refreshTokenService,
        IOptions<TeacherAuthenticationOptions> options)
    {
        _context = context;
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _options = options.Value;
    }

    public async Task<TeacherInvitationIssueResult> IssueAsync(
        long invitedByUserId,
        long communityId,
        string email,
        CancellationToken cancellationToken)
    {
        return await IssueAsync(invitedByUserId, communityId, email, CommunityUserRole.Teacher, cancellationToken);
    }

    public async Task<TeacherInvitationIssueResult> IssueCommunityAdminSetupAsync(
        long invitedByUserId,
        long communityId,
        string email,
        CancellationToken cancellationToken)
    {
        return await IssueAsync(invitedByUserId, communityId, email, CommunityUserRole.Owner, cancellationToken);
    }

    private async Task<TeacherInvitationIssueResult> IssueAsync(
        long invitedByUserId,
        long communityId,
        string email,
        CommunityUserRole role,
        CancellationToken cancellationToken)
    {
        var trimmedEmail = email.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(trimmedEmail);
        var now = DateTime.UtcNow;

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var community = await _context.Communities
            .FirstOrDefaultAsync(x => x.Id == communityId && x.Status == CommunityStatus.Active, cancellationToken)
            ?? throw Error(ErrorMessage.NotFound, HttpStatusCode.NotFound);

        if (role == CommunityUserRole.Teacher && !await HasActiveOwnerAsync(invitedByUserId, communityId, cancellationToken))
        {
            throw Error(ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
        }

        if (role == CommunityUserRole.Owner && !await IsPlatformAdminAsync(invitedByUserId, cancellationToken))
        {
            throw Error(ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user == null)
        {
            user = new User
            {
                UserName = trimmedEmail,
                NormalizedUserName = _userManager.NormalizeName(trimmedEmail),
                Email = trimmedEmail,
                NormalizedEmail = normalizedEmail,
                Name = role == CommunityUserRole.Teacher ? "Invited Teacher" : trimmedEmail,
                IsTeacherAccount = role == CommunityUserRole.Teacher,
                LockoutEnabled = true,
                Status = UserStatus.Active,
                CreatedAt = now
            };
            Ensure(await _userManager.CreateAsync(user));
        }
        else
        {
            var hasPlayer = await _context.Players.AnyAsync(x => x.UserId == user.Id, cancellationToken);
            if (user.Status != UserStatus.Active ||
                (role == CommunityUserRole.Teacher && ((!user.IsTeacherAccount && hasPlayer) || !string.IsNullOrWhiteSpace(user.GoogleId))))
            {
                throw Error(ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
            }

            if (role == CommunityUserRole.Teacher)
            {
                user.IsTeacherAccount = true;
            }
            user.UpdatedAt = now;
            Ensure(await _userManager.UpdateAsync(user));
        }

        var currentRoleMemberships = await _context.CommunityUsers
            .Where(x => x.UserId == user.Id &&
                        x.Role == role &&
                        (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (currentRoleMemberships.Any(x => x.CommunityId != communityId))
        {
            throw role == CommunityUserRole.Teacher
                ? TeacherCommunityConflict()
                : Error(ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
        }

        var membership = currentRoleMemberships.SingleOrDefault();
        if (membership?.Status == CommunityUserStatus.Active)
        {
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return IssueResult(user, community, membership, null);
        }

        if (membership == null)
        {
            membership = await _context.CommunityUsers.FirstOrDefaultAsync(
                x => x.CommunityId == communityId && x.UserId == user.Id,
                cancellationToken);

            if (membership != null && membership.Role != role)
            {
                throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
            }

            if (membership == null || membership.Status == CommunityUserStatus.Removed)
            {
                CommunityLicense? license = null;
                if (role == CommunityUserRole.Teacher)
                {
                    license = await _context.CommunityLicenses.FirstOrDefaultAsync(x => x.CommunityId == communityId, cancellationToken);
                    if (license == null || license.UsedTeachers >= license.MaxTeachers)
                    {
                        throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
                    }
                }

                if (membership == null)
                {
                    membership = new CommunityUser
                    {
                        CommunityId = communityId,
                        UserId = user.Id,
                        Role = role,
                        Status = CommunityUserStatus.Pending,
                        CreatedAt = now
                    };
                    _context.CommunityUsers.Add(membership);
                }
                else
                {
                    membership.Role = role;
                    membership.Status = CommunityUserStatus.Pending;
                    membership.UpdatedAt = now;
                }

                if (license != null)
                {
                    license.UsedTeachers++;
                    license.UpdatedAt = now;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await RevokeCurrentInvitationsAsync(membership!.Id, now, cancellationToken);

        var rawToken = SecureTokenGenerator.Generate();
        _context.TeacherInvitations.Add(new TeacherInvitation
        {
            CommunityUserId = membership.Id,
            InvitedEmail = normalizedEmail,
            TokenHash = SecureTokenGenerator.Hash(rawToken),
            ExpiresAt = now.AddDays(_options.InvitationLifetimeDays),
            CreatedAt = now,
            CreatedByUserId = invitedByUserId,
            LastSentAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return IssueResult(user, community, membership, rawToken);
    }

    public async Task<TeacherInvitationValidationResult> ValidateAsync(string rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw InvalidInvitation();
        }

        var now = DateTime.UtcNow;
        var invitation = await _context.TeacherInvitations
            .AsNoTracking()
            .Include(x => x.CommunityUser)
            .ThenInclude(x => x.Community)
            .FirstOrDefaultAsync(x => x.TokenHash == SecureTokenGenerator.Hash(rawToken), cancellationToken);

        if (!IsUsable(invitation, now))
        {
            throw InvalidInvitation();
        }

        return new TeacherInvitationValidationResult
        {
            CommunityName = invitation!.CommunityUser.Community.Name,
            MaskedEmail = MaskEmail(invitation.InvitedEmail),
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public async Task CompleteAsync(string rawToken, string name, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            throw InvalidInvitation();
        }

        var trimmedName = name.Trim();
        var now = DateTime.UtcNow;
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var invitation = await _context.TeacherInvitations
            .Include(x => x.CommunityUser)
            .ThenInclude(x => x.Community)
            .FirstOrDefaultAsync(x => x.TokenHash == SecureTokenGenerator.Hash(rawToken), cancellationToken);
        if (!IsUsable(invitation, now))
        {
            throw InvalidInvitation();
        }

        var membership = invitation!.CommunityUser;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == membership.UserId, cancellationToken);
        if (user == null ||
            user.Status != UserStatus.Active ||
            user.NormalizedEmail != invitation.InvitedEmail ||
            (membership.Role == CommunityUserRole.Teacher && !user.IsTeacherAccount) ||
            (membership.Role != CommunityUserRole.Teacher && membership.Role != CommunityUserRole.Owner))
        {
            throw InvalidInvitation();
        }

        var otherCurrentRelationships = await _context.CommunityUsers
            .Where(x => x.UserId == user.Id &&
                        x.Role == membership.Role &&
                        (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending) &&
                        x.Id != membership.Id)
            .AnyAsync(cancellationToken);
        if (otherCurrentRelationships)
        {
            throw membership.Role == CommunityUserRole.Teacher
                ? TeacherCommunityConflict()
                : Error(ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
        }

        user.Name = trimmedName;
        if (membership.Role == CommunityUserRole.Teacher)
        {
            user.IsTeacherAccount = true;
        }
        user.EmailConfirmed = true;
        user.UpdatedAt = now;
        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            user.UserName = user.Email;
            user.NormalizedUserName = _userManager.NormalizeName(user.Email);
        }

        var hadPassword = await _userManager.HasPasswordAsync(user);
        IdentityResult passwordResult;
        if (hadPassword)
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, password);
        }
        else
        {
            passwordResult = await _userManager.AddPasswordAsync(user, password);
        }
        Ensure(passwordResult);
        Ensure(await _userManager.UpdateAsync(user));

        membership.Status = CommunityUserStatus.Active;
        membership.UpdatedAt = now;
        invitation.AcceptedAt = now;
        await _context.SaveChangesAsync(cancellationToken);
        if (hadPassword)
        {
            await _refreshTokenService.RevokeAllForUserAsync(user.Id, null, cancellationToken);
        }
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public Task RevokeForMembershipAsync(long communityUserId, CancellationToken cancellationToken)
    {
        return RevokeCurrentInvitationsAsync(communityUserId, DateTime.UtcNow, cancellationToken);
    }

    private static bool IsUsable(TeacherInvitation? invitation, DateTime now, CommunityUserRole? expectedRole = null)
    {
        return invitation != null &&
               invitation.AcceptedAt == null &&
               invitation.RevokedAt == null &&
               invitation.ExpiresAt > now &&
               (invitation.CommunityUser.Role == CommunityUserRole.Teacher || invitation.CommunityUser.Role == CommunityUserRole.Owner) &&
               (!expectedRole.HasValue || invitation.CommunityUser.Role == expectedRole.Value) &&
               invitation.CommunityUser.Status == CommunityUserStatus.Pending &&
               invitation.CommunityUser.Community.Status == CommunityStatus.Active;
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
        {
            return null;
        }

        return await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    private async Task RevokeCurrentInvitationsAsync(long communityUserId, DateTime now, CancellationToken cancellationToken)
    {
        var query = _context.TeacherInvitations
            .Where(x => x.CommunityUserId == communityUserId && x.RevokedAt == null && x.AcceptedAt == null);
        if (_context.Database.IsRelational())
        {
            await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), cancellationToken);
            return;
        }

        var invitations = await query.ToListAsync(cancellationToken);
        foreach (var invitation in invitations)
        {
            invitation.RevokedAt = now;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> HasActiveOwnerAsync(long userId, long communityId, CancellationToken cancellationToken)
    {
        return _context.CommunityUsers.AnyAsync(
            x => x.UserId == userId &&
                 x.CommunityId == communityId &&
                 x.Role == CommunityUserRole.Owner &&
                 x.Status == CommunityUserStatus.Active,
            cancellationToken);
    }

    private Task<bool> IsPlatformAdminAsync(long userId, CancellationToken cancellationToken)
    {
        return _context.Users.AnyAsync(
            x => x.Id == userId && x.IsPlatformAdmin && x.Status == UserStatus.Active,
            cancellationToken);
    }

    private static TeacherInvitationIssueResult IssueResult(User user, Community community, CommunityUser membership, string? rawToken)
    {
        return new TeacherInvitationIssueResult
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            CommunityId = community.Id,
            CommunityName = community.Name,
            Status = membership.Status.ToString(),
            InvitationToken = rawToken
        };
    }

    private static string MaskEmail(string email)
    {
        email = email.ToLowerInvariant();
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "***";
        }

        var local = email[..atIndex];
        return local.Length == 1
            ? $"{local}***{email[atIndex..]}"
            : $"{local[0]}***{local[^1]}{email[atIndex..]}";
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        }
    }

    private static GenericException TeacherCommunityConflict() => new(
        ErrorCode.TeacherAlreadyBelongsToAnotherCommunity,
        ErrorMessage.TeacherAlreadyBelongsToAnotherCommunity,
        HttpStatusCode.Conflict);

    private static GenericException InvalidInvitation() => new(
        ErrorCode.InvalidTeacherInvitation,
        ErrorMessage.InvalidTeacherInvitation,
        HttpStatusCode.BadRequest);

    private static GenericException Error(string message, HttpStatusCode status) => new(ErrorCode.Failure, message, status);
}
