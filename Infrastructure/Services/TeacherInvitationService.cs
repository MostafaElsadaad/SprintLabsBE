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
        var trimmedEmail = email.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(trimmedEmail);
        var now = DateTime.UtcNow;

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var community = await _context.Communities
            .FirstOrDefaultAsync(x => x.Id == communityId && x.Status == CommunityStatus.Active, cancellationToken)
            ?? throw Error(ErrorMessage.NotFound, HttpStatusCode.NotFound);

        var activeOwner = await _context.CommunityUsers.AnyAsync(
            x => x.UserId == invitedByUserId &&
                 x.CommunityId == communityId &&
                 x.Role == CommunityUserRole.Owner &&
                 x.Status == CommunityUserStatus.Active,
            cancellationToken);
        if (!activeOwner)
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
                Name = "Invited Teacher",
                IsTeacherAccount = true,
                LockoutEnabled = true,
                Status = UserStatus.Active,
                CreatedAt = now
            };
            Ensure(await _userManager.CreateAsync(user));
        }
        else
        {
            var hasPlayer = await _context.Players.AnyAsync(x => x.UserId == user.Id, cancellationToken);
            if ((!user.IsTeacherAccount && hasPlayer) || !string.IsNullOrWhiteSpace(user.GoogleId))
            {
                throw Error(ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
            }

            user.IsTeacherAccount = true;
            user.UpdatedAt = now;
            Ensure(await _userManager.UpdateAsync(user));
        }

        var currentTeacherMemberships = await _context.CommunityUsers
            .Where(x => x.UserId == user.Id &&
                        x.Role == CommunityUserRole.Teacher &&
                        (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (currentTeacherMemberships.Any(x => x.CommunityId != communityId))
        {
            throw TeacherCommunityConflict();
        }

        var membership = currentTeacherMemberships.SingleOrDefault();
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

            if (membership != null && membership.Role != CommunityUserRole.Teacher)
            {
                throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
            }

            if (membership == null || membership.Status == CommunityUserStatus.Removed)
            {
                var license = await _context.CommunityLicenses.FirstOrDefaultAsync(x => x.CommunityId == communityId, cancellationToken);
                if (license == null || license.UsedTeachers >= license.MaxTeachers)
                {
                    throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
                }

                if (membership == null)
                {
                    membership = new CommunityUser
                    {
                        CommunityId = communityId,
                        UserId = user.Id,
                        Role = CommunityUserRole.Teacher,
                        Status = CommunityUserStatus.Pending,
                        CreatedAt = now
                    };
                    _context.CommunityUsers.Add(membership);
                }
                else
                {
                    membership.Role = CommunityUserRole.Teacher;
                    membership.Status = CommunityUserStatus.Pending;
                    membership.UpdatedAt = now;
                }

                license.UsedTeachers++;
                license.UpdatedAt = now;
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
        if (user == null || !user.IsTeacherAccount || user.Status != UserStatus.Active || user.NormalizedEmail != invitation.InvitedEmail)
        {
            throw InvalidInvitation();
        }

        var otherCurrentRelationships = await _context.CommunityUsers
            .Where(x => x.UserId == user.Id &&
                        x.Role == CommunityUserRole.Teacher &&
                        (x.Status == CommunityUserStatus.Active || x.Status == CommunityUserStatus.Pending) &&
                        x.Id != membership.Id)
            .AnyAsync(cancellationToken);
        if (otherCurrentRelationships)
        {
            throw TeacherCommunityConflict();
        }

        user.Name = trimmedName;
        user.IsTeacherAccount = true;
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

    private static bool IsUsable(TeacherInvitation? invitation, DateTime now)
    {
        return invitation != null &&
               invitation.AcceptedAt == null &&
               invitation.RevokedAt == null &&
               invitation.ExpiresAt > now &&
               invitation.CommunityUser.Role == CommunityUserRole.Teacher &&
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