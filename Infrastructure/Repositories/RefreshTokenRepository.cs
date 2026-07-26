using Domain.Models;
using Domain.Repositories;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task CreateAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRevokeAsync(
        string tokenHash,
        DateTime revokedAt,
        string? revokedByIp,
        string? replacementHash,
        CancellationToken cancellationToken)
    {
        var affected = await _context.RefreshTokens
            .Where(x => x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt)
                .SetProperty(x => x.RevokedByIp, revokedByIp)
                .SetProperty(x => x.ReplacedByTokenHash, replacementHash), cancellationToken);
        return affected == 1;
    }

    public Task RevokeAllForUserAsync(long userId, DateTime revokedAt, string? revokedByIp, CancellationToken cancellationToken)
    {
        return _context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt)
                .SetProperty(x => x.RevokedByIp, revokedByIp), cancellationToken);
    }
}
