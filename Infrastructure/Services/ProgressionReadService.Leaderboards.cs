using Domain.Enums;
using Domain.Models;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using Shared.Responses;

namespace Infrastructure.Services;

public partial class ProgressionReadService
{
    public async Task<LeaderboardPageResponse> GetLeaderboardAsync(long userId, long? communityId,
        LeaderboardRequest page, CancellationToken ct)
    {
        var (query, period, start, end, filters) = await LeaderboardQueryAsync(userId, communityId, page, ct);
        var total = await query.CountAsync(ct);
        var offset = (page.Page - 1) * page.PageSize;
        var rows = await query.OrderByDescending(x => x.Points).ThenBy(x => x.PlayerProfileId)
            .Skip(offset).Take(page.PageSize).ToListAsync(ct);
        for (var i = 0; i < rows.Count; i++) rows[i].Position = (long)offset + i + 1;
        var playerId = await OwnPlayerIdAsync(userId, ct);
        return new() { Items = rows, Page = page.Page, PageSize = page.PageSize, Total = total,
            Period = period, PeriodStartsAt = start, PeriodEndsAt = end,
            CurrentPlayer = await StandingAsync(query, playerId, ct), AvailableFilters = filters };
    }

    public async Task<LeaderboardStandingResponse> GetLeaderboardStandingAsync(long userId, long? communityId,
        LeaderboardRequest request, CancellationToken ct)
    {
        var (query, period, start, end, filters) = await LeaderboardQueryAsync(userId, communityId, request, ct);
        var playerId = await OwnPlayerIdAsync(userId, ct);
        if (!playerId.HasValue) throw NotFound();
        return new() { Period = period, PeriodStartsAt = start, PeriodEndsAt = end,
            Total = await query.CountAsync(ct), CurrentPlayer = await StandingAsync(query, playerId, ct), AvailableFilters = filters };
    }

    private Task<long?> OwnPlayerIdAsync(long userId, CancellationToken ct)
        => _context.Players.AsNoTracking().Where(x => x.UserId == userId).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct);

    private static async Task<LeaderboardEntryResponse?> StandingAsync(IQueryable<LeaderboardEntryResponse> query,
        long? playerId, CancellationToken ct)
    {
        if (!playerId.HasValue) return null;
        var row = await query.SingleOrDefaultAsync(x => x.PlayerProfileId == playerId, ct);
        if (row == null) return null;
        row.Position = 1 + await query.LongCountAsync(x => x.Points > row.Points ||
            (x.Points == row.Points && x.PlayerProfileId < row.PlayerProfileId), ct);
        return row;
    }

    private async Task<(IQueryable<LeaderboardEntryResponse> Query, string Period, DateTime? Start, DateTime? End,
        List<LeaderboardFilterResponse> Filters)>
        LeaderboardQueryAsync(long userId, long? communityId, LeaderboardRequest request, CancellationToken ct)
    {
        ValidatePage(request);
        var now = _clock.GetUtcNow().UtcDateTime;
        var (period, start, end) = LeaderboardPeriod(request.Period, now);
        var user = await CallerAsync(userId, ct);
        var filters = new List<LeaderboardFilterResponse>();
        var players = _context.Players.AsNoTracking().Where(p => p.UserId.HasValue &&
            _context.Users.Any(u => u.Id == p.UserId && u.Status == UserStatus.Active));
        if (communityId.HasValue)
        {
            var (licenses, availableFilters) = await LeaderboardLicensesAsync(user, communityId.Value, request, ct);
            filters = availableFilters;
            players = players.Where(p => licenses.Any(l => l.PlayerProfileId == p.Id && l.UserId == p.UserId));
        }
        else if (request.GradeId.HasValue || request.ClassId.HasValue) throw Invalid();

        var rewards = _context.MatchRewardResults.AsNoTracking().Where(r => r.Match.Status == MatchStatus.Completed &&
            r.Match.MatchType == Domain.Enums.MatchType.Ranked && r.Match.CompletedAt >= start &&
            r.Match.CompletedAt < end && r.Match.CompletedAt <= now &&
            (!communityId.HasValue || r.Match.CommunityId == communityId));
        if (start.HasValue) players = players.Where(p => rewards.Any(r => r.PlayerProfileId == p.Id));
        var query = players.Select(p => new LeaderboardEntryResponse
        {
            PlayerProfileId = p.Id, Name = p.Name, AvatarUrl = p.AvatarUrl, Rp = p.Rp,
            Points = start.HasValue
                ? rewards.Where(r => r.PlayerProfileId == p.Id).Sum(r => (long?)r.RpChange) ?? 0
                : (long)p.Rp,
            RankTier = p.RankTier.ToString(), Level = p.Level, TotalWins = p.TotalWins
        });
        return (query, period, start, end, filters);
    }

    private async Task<(IQueryable<StudentLicense> Licenses, List<LeaderboardFilterResponse> Filters)> LeaderboardLicensesAsync(User user, long communityId,
        LeaderboardRequest request, CancellationToken ct)
    {
        if (communityId <= 0) throw Invalid();
        if (!await _context.Communities.AnyAsync(x => x.Id == communityId && x.Status == CommunityStatus.Active, ct)) throw NotFound();
        if (user.IsPlatformAdmin || await _access.HasCommunityRole(user.Id, communityId,
                new[] { CommunityUserRole.Owner, CommunityUserRole.Teacher }))
        {
            var staffLicenses = await ScopedLicensesAsync(user, communityId, ct);
            return (await FilterLicensesAsync(staffLicenses, communityId, request, ct), await LeaderboardFiltersAsync(staffLicenses, ct));
        }

        var licenses = _context.StudentLicenses.AsNoTracking().Where(x => x.CommunityId == communityId &&
            x.Status == StudentLicenseStatus.Active && x.PlayerProfileId.HasValue && x.Class.Status == ClassStatus.Active &&
            x.Class.CommunityId == communityId && x.Grade.CommunityId == communityId && x.Class.GradeId == x.GradeId);
        var ownLicenses = licenses.Where(x => x.UserId == user.Id &&
            _context.Players.Any(p => p.Id == x.PlayerProfileId && p.UserId == user.Id));
        if (!await ownLicenses.AnyAsync(ct)) throw Forbidden();
        var filtered = await FilterLicensesAsync(licenses, communityId, request, ct);
        if (request.GradeId.HasValue && !await ownLicenses.AnyAsync(x => x.GradeId == request.GradeId, ct)) throw Forbidden();
        if (request.ClassId.HasValue && !await ownLicenses.AnyAsync(x => x.ClassId == request.ClassId &&
                (!request.GradeId.HasValue || x.GradeId == request.GradeId), ct)) throw Forbidden();
        return (filtered, await LeaderboardFiltersAsync(ownLicenses, ct));
    }

    private static async Task<List<LeaderboardFilterResponse>> LeaderboardFiltersAsync(IQueryable<StudentLicense> licenses, CancellationToken ct)
    {
        var rows = await licenses.Select(x => new { x.GradeId, GradeName = x.Grade.Name, x.ClassId, ClassName = x.Class.Name })
            .Distinct().OrderBy(x => x.GradeId).ThenBy(x => x.ClassId).ToListAsync(ct);
        return rows.Select(x => new LeaderboardFilterResponse { GradeId = x.GradeId, GradeName = x.GradeName,
            ClassId = x.ClassId, ClassName = x.ClassName }).ToList();
    }

    private static (string Period, DateTime? Start, DateTime? End) LeaderboardPeriod(string? period, DateTime now)
    {
        if (string.Equals(period, "AllTime", StringComparison.OrdinalIgnoreCase)) return ("AllTime", null, null);
        if (string.Equals(period, "Week", StringComparison.OrdinalIgnoreCase))
        {
            var start = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            return ("Week", start, start.AddDays(7));
        }
        if (string.Equals(period, "Month", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return ("Month", start, start.AddMonths(1));
        }
        if (string.Equals(period, "Year", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return ("Year", start, start.AddYears(1));
        }
        throw Invalid();
    }
}
