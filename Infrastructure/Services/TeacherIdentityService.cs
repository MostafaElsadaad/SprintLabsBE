using System.Net;

using Domain.Enums;
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

    public async Task<TeacherRegistrationResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(email);
        var now = DateTime.UtcNow;
        var user = await FindByNormalizedEmailAsync(normalized, cancellationToken);
        if (user == null)
        {
            user = NewTeacher(name, email, normalized, now);
            var created = await _userManager.CreateAsync(user, password);
            EnsureSucceeded(created, HttpStatusCode.BadRequest);
        }
        else
        {
            var canReuse = user.IsTeacherAccount
                && string.IsNullOrWhiteSpace(user.GoogleId)
                && !await _userManager.HasPasswordAsync(user);
            if (!canReuse)
            {
                throw Error(ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
            }

            user.Name = name.Trim();
            user.IsTeacherAccount = true;
            user.EmailConfirmed = false;
            user.LockoutEnabled = true;
            user.LastConfirmationEmailSentAt = now;
            user.UpdatedAt = now;
            var addPassword = await _userManager.AddPasswordAsync(user, password);
            EnsureSucceeded(addPassword, HttpStatusCode.BadRequest);
            var updated = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updated, HttpStatusCode.BadRequest);
        }

        user.LastConfirmationEmailSentAt = now;
        var update = await _userManager.UpdateAsync(user);
        EnsureSucceeded(update, HttpStatusCode.BadRequest);
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return new TeacherRegistrationResult
        {
            UserId = user.Id,
            Email = user.Email ?? email.Trim(),
            Name = user.Name,
            ConfirmationToken = token,
            ResendAvailableAt = now.AddSeconds(_options.ConfirmationResendCooldownSeconds)
        };
    }

    public async Task ConfirmEmailAsync(long userId, string token, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsTeacherAccount) throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        if (user.EmailConfirmed) return;
        var result = await _userManager.ConfirmEmailAsync(user, token);
        EnsureSucceeded(result, HttpStatusCode.BadRequest);
    }

    public async Task<ConfirmationDispatchResult> ResendConfirmationAsync(string email, CancellationToken cancellationToken)
    {
        var user = await FindByNormalizedEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user == null || !user.IsTeacherAccount || user.EmailConfirmed || !await _userManager.HasPasswordAsync(user))
        {
            return new ConfirmationDispatchResult();
        }

        var now = DateTime.UtcNow;
        var next = user.LastConfirmationEmailSentAt?.AddSeconds(_options.ConfirmationResendCooldownSeconds);
        if (next.HasValue && next.Value > now)
        {
            return new ConfirmationDispatchResult { ResendAvailableAt = next };
        }

        user.LastConfirmationEmailSentAt = now;
        user.UpdatedAt = now;
        EnsureSucceeded(await _userManager.UpdateAsync(user), HttpStatusCode.BadRequest);
        return new ConfirmationDispatchResult
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            ConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user),
            ResendAvailableAt = now.AddSeconds(_options.ConfirmationResendCooldownSeconds)
        };
    }

    public async Task<TeacherIdentityResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await FindByNormalizedEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user == null || !user.IsTeacherAccount || user.Status != UserStatus.Active || !user.EmailConfirmed || !await _userManager.HasPasswordAsync(user))
        {
            throw Error(ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw Error(ErrorMessage.InvalidAccessToken, result.IsLockedOut ? HttpStatusCode.Locked : HttpStatusCode.Unauthorized);
        }

        return Map(user);
    }

    public async Task<PasswordResetDispatchResult> CreatePasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var user = await FindByNormalizedEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user == null || !user.IsTeacherAccount || user.Status != UserStatus.Active || !user.EmailConfirmed || !await _userManager.HasPasswordAsync(user))
        {
            return new PasswordResetDispatchResult();
        }

        return new PasswordResetDispatchResult
        {
            Email = user.Email,
            Name = user.Name,
            ResetToken = await _userManager.GeneratePasswordResetTokenAsync(user)
        };
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        var user = await FindByNormalizedEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user == null || !user.IsTeacherAccount || !await _userManager.HasPasswordAsync(user)) throw Error(ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        EnsureSucceeded(result, HttpStatusCode.BadRequest);
        await _refreshTokenService.RevokeAllForUserAsync(user.Id, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TeacherIdentityResult?> GetTeacherAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsTeacherAccount, cancellationToken);
        return user == null ? null : Map(user);
    }

    private async Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await _userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    private User NewTeacher(string name, string email, string normalizedEmail, DateTime now) => new()
    {
        UserName = email.Trim(),
        NormalizedUserName = _userManager.NormalizeName(email.Trim()),
        Email = email.Trim(),
        NormalizedEmail = normalizedEmail,
        Name = name.Trim(),
        IsTeacherAccount = true,
        EmailConfirmed = false,
        LockoutEnabled = true,
        Status = UserStatus.Active,
        CreatedAt = now
    };

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
    private static TeacherIdentityResult Map(User user) => new() { UserId = user.Id, Name = user.Name, Email = user.Email ?? string.Empty };
    private static void EnsureSucceeded(IdentityResult result, HttpStatusCode statusCode)
    {
        if (!result.Succeeded) throw Error(ErrorMessage.InvalidInput, statusCode);
    }
    private static GenericException Error(string message, HttpStatusCode statusCode) => new(ErrorCode.Failure, message, statusCode);
}
