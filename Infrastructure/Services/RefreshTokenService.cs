using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Services.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Shared.Options;
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
        var hash = SecureTokenGenerator.Hash(rawToken);
        var current = await _repository.GetByHashAsync(hash, cancellationToken);
        var now = DateTime.UtcNow;
        if (current == null || current.RevokedAt != null || current.ExpiresAt <= now) return null;

        var eligible = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == current.UserId && x.IsTeacherAccount && x.EmailConfirmed && x.Status == UserStatus.Active, cancellationToken);
        if (!eligible) return null;

        var rawReplacement = SecureTokenGenerator.Generate();
        var replacementHash = SecureTokenGenerator.Hash(rawReplacement);
        if (!await _repository.TryRevokeAsync(hash, now, revokedByIp, replacementHash, cancellationToken)) return null;

        var result = new RefreshTokenResult
        {
            UserId = current.UserId,
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
