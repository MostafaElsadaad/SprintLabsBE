using Microsoft.EntityFrameworkCore;
using Shared.Responses;

namespace Infrastructure.Services;
public partial class MissionService
{
    public async Task<List<PlayerMissionResponse>> HistoryAsync(long userId, int page, int pageSize, CancellationToken ct)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100) throw Invalid();
        var playerId = await MyPlayerAsync(userId, ct);
        // History is read-only: it must not assign missions, expire them or grant rewards.
        var rows = await _context.PlayerMissions.AsNoTracking()
            .Where(x => x.PlayerProfileId == playerId && x.CompletedAt != null)
            .OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }
}
