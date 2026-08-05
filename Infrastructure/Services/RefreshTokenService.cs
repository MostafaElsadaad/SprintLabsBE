using Domain.Enums;
using System.Data;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Services.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Shared.Options;
using Shared.Enums;
using Shared.Responses;

namespace Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly JWTOptions _options;

    public RefreshTokenService(
        IRefreshTokenRepository repository,
        ApplicationDbContext context,
        IOptions<JWTOptions> options)
    {
        _repository = repository;
        _context = context;
        _options = options.Value;
    }

    public async Task<RefreshTokenResult> IssueAsync(long userId, string? createdByIp, CancellationToken cancellationToken)
    {
        var raw = SecureTokenGenerator.Generate();
        var now = DateTime.UtcNow;
        var result = new RefreshTokenResult
        {
            UserId = userId,
            RefreshToken = raw,
            RefreshTokenExpiresAt = now.AddDays(_options.RefreshTokenLifetimeDays)
        };
        await _repository.CreateAsync(new RefreshToken
        {
            UserId = userId,
            TokenHash = SecureTokenGenerator.Hash(raw),
            CreatedAt = now,
            ExpiresAt = result.RefreshTokenExpiresAt,
            CreatedByIp = createdByIp
        }, cancellationToken);
        return result;
    }

    public async Task<RefreshTokenResult?> RotateAsync(string rawToken, string? revokedByIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return null;
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var hash = SecureTokenGenerator.Hash(rawToken);
        var current = await _repository.GetByHashAsync(hash, cancellationToken);
        var now = DateTime.UtcNow;
        if (current == null || current.RevokedAt != null || current.ExpiresAt <= now) return null;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == current.UserId && x.Status == UserStatus.Active && x.EmailConfirmed, cancellationToken);
        if (user == null) return null;

        var memberships = await _context.CommunityUsers
            .AsNoTracking()
            .Where(x => x.UserId == user.Id &&
                        (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher))
            .Select(x => new { x.Role, x.Status, CommunityStatus = x.Community.Status })
            .ToListAsync(cancellationToken);
        var activeMemberships = memberships
            .Where(x => x.Status == CommunityUserStatus.Active && x.CommunityStatus == CommunityStatus.Active)
            .ToList();
        if (!user.IsPlatformAdmin &&
            (activeMemberships.Count != 1 || memberships.Any(x => x.Status == CommunityUserStatus.Pending)))
        {
            return null;
        }

        var rawReplacement = SecureTokenGenerator.Generate();
        var replacementHash = SecureTokenGenerator.Hash(rawReplacement);
        if (!await _repository.TryRevokeAsync(hash, now, revokedByIp, replacementHash, cancellationToken)) return null;

        var result = new RefreshTokenResult
        {
            UserId = current.UserId,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            AccountType = user.IsPlatformAdmin
                ? AuthenticatedAccountType.PlatformAdmin
                : activeMemberships[0].Role == CommunityUserRole.Owner
                    ? AuthenticatedAccountType.CommunityAdmin
                    : AuthenticatedAccountType.Teacher,
            RefreshToken = rawReplacement,
            RefreshTokenExpiresAt = now.AddDays(_options.RefreshTokenLifetimeDays)
        };
        await _repository.CreateAsync(new RefreshToken
        {
            UserId = current.UserId,
            TokenHash = replacementHash,
            CreatedAt = now,
            ExpiresAt = result.RefreshTokenExpiresAt,
            CreatedByIp = revokedByIp
        }, cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return result;
    }

    public async Task RevokeAsync(string rawToken, string? revokedByIp, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await _repository.TryRevokeAsync(SecureTokenGenerator.Hash(rawToken), DateTime.UtcNow, revokedByIp, null, cancellationToken);
        }
    }

    public Task RevokeAllForUserAsync(long userId, string? revokedByIp, CancellationToken cancellationToken)
    {
        return _repository.RevokeAllForUserAsync(userId, DateTime.UtcNow, revokedByIp, cancellationToken);
    }
}
