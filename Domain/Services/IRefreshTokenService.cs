using Shared.Responses;

namespace Domain.Services;

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> IssueAsync(long userId, string? createdByIp, CancellationToken cancellationToken);
    Task<RefreshTokenResult?> RotateAsync(string rawToken, string? revokedByIp, CancellationToken cancellationToken);
    Task RevokeAsync(string rawToken, string? revokedByIp, CancellationToken cancellationToken);
    Task RevokeAllForUserAsync(long userId, string? revokedByIp, CancellationToken cancellationToken);
}
