using Application.Features.CommunityDashboard.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Communities.Students.Common;

public class StudentRosterService
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;
    private readonly IBaseRepository<MatchPlayer> _matches;

    public StudentRosterService(DashboardAuthorization authorization, DashboardProjection projection, IBaseRepository<MatchPlayer> matches)
    { _authorization = authorization; _projection = projection; _matches = matches; }

    public async Task<List<StudentView>> ReadAsync(long userId, long? classId, long? gradeId, string? search, string? status, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(userId, false, ct);
        var normalized = status?.Trim().ToUpperInvariant();
        if (normalized != null && normalized is not ("ACTIVE" or "PENDING" or "INACTIVE")) throw DashboardAuthorization.Invalid();
        if (gradeId.HasValue && gradeId <= 0) throw DashboardAuthorization.Invalid();
        var rows = await _projection.StudentsAsync(scope, classId, ct, includeRevoked: true);
        rows = rows.Where(x => (!gradeId.HasValue || x.Grade.Id == gradeId) &&
            (normalized == null || x.Status == normalized) && (string.IsNullOrWhiteSpace(search) ||
            x.FullName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) || x.Email.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id).ToList();
        var performance = await PerformanceAsync(scope.CommunityId, rows.Select(x => x.PlayerProfileId).OfType<long>(), ct);
        foreach (var row in rows)
        {
            var p = row.PlayerProfileId.HasValue ? performance.GetValueOrDefault(row.PlayerProfileId.Value) : null;
            row.AvgScore = p?.AverageScore ?? 0; row.SessionsCount = p?.MatchesPlayed ?? 0;
        }
        return rows;
    }

    public async Task<Dictionary<long, StudentPerformance>> PerformanceAsync(long communityId, IEnumerable<long> playerIds, CancellationToken ct)
    {
        var ids = playerIds.Distinct().ToArray();
        if (ids.Length == 0) return new();
        var matches = await _matches.AsQueryable().AsNoTracking().Where(x => ids.Contains(x.PlayerProfileId) &&
            x.CommunityId == communityId && x.Match.CommunityId == communityId && x.Match.Status == MatchStatus.Completed)
            .Select(x => new { x.PlayerProfileId, x.IsWinner, x.CorrectAnswers, x.WrongAnswers,
                At = x.Match.CompletedAt ?? x.Match.EndedAt }).ToListAsync(ct);
        return matches.GroupBy(x => x.PlayerProfileId).ToDictionary(g => g.Key, g =>
        {
            var correct = g.Sum(x => (long)x.CorrectAnswers); var wrong = g.Sum(x => (long)x.WrongAnswers);
            return new StudentPerformance { MatchesPlayed = g.Count(), Wins = g.Count(x => x.IsWinner),
                CorrectAnswers = correct, WrongAnswers = wrong,
                AverageScore = correct + wrong == 0 ? 0 : Math.Round(correct * 100m / (correct + wrong), 2),
                WinRate = Math.Round(g.Count(x => x.IsWinner) * 100m / g.Count(), 2), LastActivityAt = g.Max(x => x.At) };
        });
    }
}
