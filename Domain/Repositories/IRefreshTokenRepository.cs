using Domain.Models;

namespace Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task CreateAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<bool> TryRevokeAsync(string tokenHash, DateTime revokedAt, string? revokedByIp, string? replacementHash, CancellationToken cancellationToken);
    Task RevokeAllForUserAsync(long userId, DateTime revokedAt, string? revokedByIp, CancellationToken cancellationToken);
}
