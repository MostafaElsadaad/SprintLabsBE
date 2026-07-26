using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Services.Common;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    private readonly TeacherAuthenticationOptions _options;
    public TeacherInvitationService(ApplicationDbContext context, UserManager<User> userManager, IOptions<TeacherAuthenticationOptions> options)
    { _context = context; _userManager = userManager; _options = options.Value; }

    public async Task<TeacherInvitationIssueResult> IssueAsync(long invitedByUserId, long communityId, string email, string name, CancellationToken cancellationToken)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email.Trim());
        var now = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var community = await _context.Communities.FirstOrDefaultAsync(x => x.Id == communityId, cancellationToken) ?? throw Error(ErrorMessage.NotFound, HttpStatusCode.NotFound);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user == null)
        {
            user = new User { UserName = email.Trim(), NormalizedUserName = _userManager.NormalizeName(email.Trim()), Email = email.Trim(), NormalizedEmail = normalizedEmail, Name = name.Trim(), IsTeacherAccount = true, LockoutEnabled = true, Status = UserStatus.Active, CreatedAt = now };
            Ensure(await _userManager.CreateAsync(user));
        }
        else if (!user.IsTeacherAccount && (!string.IsNullOrWhiteSpace(user.GoogleId) || await _context.Players.AnyAsync(x => x.UserId == user.Id, cancellationToken)))
        {
            throw Error(ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
        }
        else
        {
            user.IsTeacherAccount = true;
            user.UpdatedAt = now;
            Ensure(await _userManager.UpdateAsync(user));
        }

        var membership = await _context.CommunityUsers.FirstOrDefaultAsync(x => x.CommunityId == communityId && x.UserId == user.Id, cancellationToken);
        if (membership != null && membership.Role != CommunityUserRole.Teacher) throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        if (membership?.Status == CommunityUserStatus.Active)
        {
            await transaction.CommitAsync(cancellationToken);
            return new TeacherInvitationIssueResult { UserId = user.Id, Name = user.Name, Email = user.Email ?? email, CommunityId = communityId, CommunityName = community.Name, Status = CommunityUserStatus.Active.ToString() };
        }

        if (membership == null || membership.Status == CommunityUserStatus.Removed)
        {
            var license = await _context.CommunityLicenses.FirstOrDefaultAsync(x => x.CommunityId == communityId, cancellationToken);
            if (license == null || license.UsedTeachers >= license.MaxTeachers) throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
            if (membership == null)
            {
                membership = new CommunityUser { CommunityId = communityId, UserId = user.Id, Role = CommunityUserRole.Teacher, Status = CommunityUserStatus.Pending, CreatedAt = now };
                _context.CommunityUsers.Add(membership);
            }
            else
            {
                membership.Status = CommunityUserStatus.Pending;
                membership.UpdatedAt = now;
            }
            license.UsedTeachers++;
            license.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _context.TeacherInvitations
            .Where(x => x.CommunityUserId == membership!.Id && x.RevokedAt == null && x.AcceptedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), cancellationToken);
        var raw = SecureTokenGenerator.Generate();
        _context.TeacherInvitations.Add(new TeacherInvitation { CommunityUserId = membership.Id, InvitedEmail = normalizedEmail, TokenHash = SecureTokenGenerator.Hash(raw), ExpiresAt = now.AddDays(_options.InvitationLifetimeDays), CreatedAt = now, CreatedByUserId = invitedByUserId, LastSentAt = now });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new TeacherInvitationIssueResult { UserId = user.Id, Name = user.Name, Email = user.Email ?? email, CommunityId = communityId, CommunityName = community.Name, Status = CommunityUserStatus.Pending.ToString(), InvitationToken = raw };
    }

    public async Task<TeacherInvitationAcceptanceResponse> AcceptAsync(long userId, string rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        var now = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var invitation = await _context.TeacherInvitations.Include(x => x.CommunityUser).ThenInclude(x => x.Community)
            .FirstOrDefaultAsync(x => x.TokenHash == SecureTokenGenerator.Hash(rawToken), cancellationToken);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (invitation == null || user == null || !user.IsTeacherAccount || !user.EmailConfirmed || user.Status != UserStatus.Active || user.NormalizedEmail != invitation.InvitedEmail)
            throw Error(ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
        var membership = invitation.CommunityUser;
        if (invitation.AcceptedAt != null && membership.Status == CommunityUserStatus.Active) return Map(membership);
        if (invitation.RevokedAt != null || invitation.ExpiresAt <= now || membership.Role != CommunityUserRole.Teacher || membership.Status != CommunityUserStatus.Pending)
            throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        membership.Status = CommunityUserStatus.Active;
        membership.UpdatedAt = now;
        invitation.AcceptedAt = now;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(membership);
    }

    public Task RevokeForMembershipAsync(long communityUserId, CancellationToken cancellationToken)
    {
        return _context.TeacherInvitations.Where(x => x.CommunityUserId == communityUserId && x.RevokedAt == null && x.AcceptedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTime.UtcNow), cancellationToken);
    }

    private static TeacherInvitationAcceptanceResponse Map(CommunityUser membership) => new() { CommunityId = membership.CommunityId, CommunityName = membership.Community.Name, Role = membership.Role.ToString(), Status = membership.Status.ToString() };
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest); }
    private static GenericException Error(string message, HttpStatusCode status) => new(ErrorCode.Failure, message, status);
}
