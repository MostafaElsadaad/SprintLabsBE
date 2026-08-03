using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;
using Shared.Responses;

namespace Infrastructure.Services;

public class TeacherIdentityService : ITeacherIdentityService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ApplicationDbContext _context;
    private readonly TeacherAuthenticationOptions _options;

    public TeacherIdentityService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IRefreshTokenService refreshTokenService,
        ApplicationDbContext context,
        IOptions<TeacherAuthenticationOptions> options)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _refreshTokenService = refreshTokenService;
        _context = context;
        _options = options.Value;
    }

    public async Task<TeacherIdentityResult> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken)
    {
        var user = await FindByIdentifierAsync(identifier, cancellationToken);
        if (user == null ||
            !user.IsTeacherAccount ||
            user.Status != UserStatus.Active ||
            !user.EmailConfirmed ||
            !await _userManager.HasPasswordAsync(user))
        {
            throw InvalidCredentials();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            throw new GenericException(
                ErrorCode.Failure,
                ErrorMessage.InvalidAccessToken,
                signInResult.IsLockedOut ? HttpStatusCode.Locked : HttpStatusCode.Unauthorized);
        }

        return Map(user);
    }

    public async Task<PasswordResetDispatchResult> CreatePasswordResetAsync(string identifier, CancellationToken cancellationToken)
    {
        var user = await FindByIdentifierAsync(identifier, cancellationToken);
        if (user == null ||
            !user.IsTeacherAccount ||
            user.Status != UserStatus.Active ||
            !user.EmailConfirmed ||
            !await _userManager.HasPasswordAsync(user) ||
            !await HasExactlyOneActiveTeacherCommunityAsync(user.Id, cancellationToken))
        {
            return new PasswordResetDispatchResult();
        }

        var now = DateTime.UtcNow;
        var cooldownStart = now.AddSeconds(-_options.PasswordResetResendCooldownSeconds);
        var claimed = _context.Database.IsRelational()
            ? await _context.Users
                .Where(x => x.Id == user.Id &&
                            (x.LastPasswordResetEmailSentAt == null || x.LastPasswordResetEmailSentAt <= cooldownStart))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LastPasswordResetEmailSentAt, now)
                    .SetProperty(x => x.UpdatedAt, now), cancellationToken)
            : await ClaimPasswordResetCooldownInMemoryAsync(user.Id, cooldownStart, now, cancellationToken);
        if (claimed != 1)
        {
            return new PasswordResetDispatchResult();
        }

        return new PasswordResetDispatchResult
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            ResetToken = await _userManager.GeneratePasswordResetTokenAsync(user)
        };
    }

    public async Task ResetPasswordAsync(long userId, string token, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsTeacherAccount || !await _userManager.HasPasswordAsync(user))
        {
            throw InvalidResetToken();
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            throw InvalidResetToken();
        }

        await _refreshTokenService.RevokeAllForUserAsync(user.Id, null, cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task<TeacherIdentityResult?> GetTeacherAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsTeacherAccount, cancellationToken);
        return user == null ? null : Map(user);
    }

    private async Task<User?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var value = identifier.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(value);
        var normalizedUserName = _userManager.NormalizeName(value);
        var users = await _userManager.Users
            .Where(x => x.NormalizedEmail == normalizedEmail || x.NormalizedUserName == normalizedUserName)
            .Take(2)
            .ToListAsync(cancellationToken);

        return users.Count == 1 ? users[0] : null;
    }

    private async Task<int> ClaimPasswordResetCooldownInMemoryAsync(long userId, DateTime cooldownStart, DateTime now, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user == null || (user.LastPasswordResetEmailSentAt != null && user.LastPasswordResetEmailSentAt > cooldownStart))
        {
            return 0;
        }

        user.LastPasswordResetEmailSentAt = now;
        user.UpdatedAt = now;
        await _context.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private async Task<bool> HasExactlyOneActiveTeacherCommunityAsync(long userId, CancellationToken cancellationToken)
    {
        var memberships = await _context.CommunityUsers
            .Where(x => x.UserId == userId && x.Role == CommunityUserRole.Teacher)
            .Select(x => new { x.Status, CommunityStatus = x.Community.Status })
            .ToListAsync(cancellationToken);

        return memberships.Count(x => x.Status == CommunityUserStatus.Active && x.CommunityStatus == CommunityStatus.Active) == 1 &&
               memberships.All(x => x.Status != CommunityUserStatus.Pending);
    }

    private static TeacherIdentityResult Map(User user) => new()
    {
        UserId = user.Id,
        Name = user.Name,
        Email = user.Email ?? string.Empty
    };

    private static GenericException InvalidCredentials() => new(
        ErrorCode.Failure,
        ErrorMessage.InvalidAccessToken,
        HttpStatusCode.Unauthorized);

    private static GenericException InvalidResetToken() => new(
        ErrorCode.InvalidOrExpiredPasswordResetToken,
        ErrorMessage.InvalidOrExpiredPasswordResetToken,
        HttpStatusCode.BadRequest);
}