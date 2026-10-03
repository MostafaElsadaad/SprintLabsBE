using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using Shared.Responses;
using System.Text.Json;
namespace Infrastructure.Services;
public partial class MissionService
{
    public Task<List<MissionEventResponse>> EventsAsync(List<MissionEventRequest> events, CancellationToken ct)
    {
        if (events == null || events.Count is < 1 or > 100 || events.Any(x => x == null)) throw Invalid();
        foreach (var e in events)
        {
            if (string.IsNullOrWhiteSpace(e.EventId) || e.EventId.Length > 100 || e.EventId.Trim() != e.EventId || e.PlayerProfileId <= 0 ||
                e.OccurredAt.Kind != DateTimeKind.Utc || e.OccurredAt > DateTime.UtcNow.AddMinutes(5) ||
                e.CommunityId is <= 0 || e.MatchId is <= 0) throw Invalid();
            Parse<MissionEventType>(e.EventType); ValidateData(e.EventData);
        }
        return AtomicAsync(events.Select(x => x.PlayerProfileId), async players => {
            var responses = new List<MissionEventResponse>();
            var now = DateTime.UtcNow;
            foreach (var e in events)
            {
                var type = Parse<MissionEventType>(e.EventType);
                var data = Canonical(e.EventData); var time = Stamp(e.OccurredAt);
                var saved = await _context.MissionEventLogs.SingleOrDefaultAsync(x => x.PlayerProfileId == e.PlayerProfileId && x.EventId == e.EventId, ct);
                if (saved != null)
                {
                    if (saved.EventType != type || saved.OccurredAt != time || saved.MatchId != e.MatchId || saved.CommunityId != e.CommunityId || saved.EventDataJson != data) throw Conflict();
                    var replay = Read<MissionEventResponse>(saved.ResultJson); replay.Duplicate = true; responses.Add(replay); continue;
                }
                if (e.OccurredAt < now.AddDays(-1)) throw Invalid();
                await ValidateContextAsync(e, ct);
                await ExpireAsync(players[e.PlayerProfileId], now, ct);
                await AssignAsync(e.PlayerProfileId, now, ct);
                await _context.SaveChangesAsync(ct);
                var missions = await _context.PlayerMissions.Where(x => x.PlayerProfileId == e.PlayerProfileId &&
                    (x.Status == PlayerMissionStatus.Assigned || x.Status == PlayerMissionStatus.InProgress) &&
                    _context.MissionActivations.Any(a => a.Id == x.MissionActivationId && a.Status == MissionActivationStatus.Active &&
                        a.StartsAt <= time && a.StartsAt <= now && (a.EndsAt == null || (a.EndsAt > now && a.EndsAt > time)))).OrderBy(x => x.Id).ToListAsync(ct);
                var result = new MissionEventResponse { EventId = e.EventId };
                foreach (var m in missions)
                {
                    var r = Read<MissionDefinitionSnapshot>(m.DefinitionSnapshotJson).Rules;
                    if (Parse<MissionEventType>(r.EventType) != type && (r.ResetEventType == null || Parse<MissionEventType>(r.ResetEventType) != type)) continue;
                    if (m.LastEventAt.HasValue && time < m.LastEventAt.Value) throw Conflict();
                    if (Apply(m, r, e, type)) result.UpdatedMissions.Add(Map(m));
                }
                _context.MissionEventLogs.Add(new MissionEventLog { PlayerProfileId = e.PlayerProfileId, EventId = e.EventId,
                    CommunityId = e.CommunityId, MatchId = e.MatchId, EventType = type, OccurredAt = time,
                    CreatedAt = now, EventDataJson = data, ResultJson = Json(result) });
                // Flush each event inside the transaction so duplicate keys within one bulk request replay too.
                await _context.SaveChangesAsync(ct);
                responses.Add(result);
            }
            return responses;
        }, ct, events.Where(x => x.MatchId.HasValue).Select(x => x.MatchId!.Value));
    }
    private async Task ValidateContextAsync(MissionEventRequest e, CancellationToken ct)
    {
        if (e.MatchId.HasValue)
        {
            if (!await _context.Matches.AnyAsync(m => m.Id == e.MatchId && m.CommunityId == e.CommunityId &&
                (m.Status == MatchStatus.Started || m.Status == MatchStatus.Completed) && m.StartedAt <= e.OccurredAt &&
                (m.EndedAt == null || m.EndedAt >= e.OccurredAt) && m.MatchPlayers.Any(p => p.PlayerProfileId == e.PlayerProfileId), ct)) throw Missing();
        }
        else if (e.CommunityId.HasValue && !await _context.StudentLicenses.AnyAsync(l => l.CommunityId == e.CommunityId &&
            l.PlayerProfileId == e.PlayerProfileId && l.Status == StudentLicenseStatus.Active && l.Class.Status == ClassStatus.Active &&
            l.Class.CommunityId == e.CommunityId && _context.Communities.Any(c => c.Id == e.CommunityId && c.Status == CommunityStatus.Active) &&
            _context.Players.Any(p => p.Id == e.PlayerProfileId && p.UserId == l.UserId), ct)) throw Missing();
    }
    private static bool Apply(PlayerMission m, MissionTemplateRequest r, MissionEventRequest e, MissionEventType type)
    {
        var progressType = Parse<MissionProgressType>(r.ProgressType);
        var state = Read<MissionProgressState>(m.ProgressStateJson);
        var old = m.CurrentProgress;
        if (r.MatchScoped)
        {
            if (!e.MatchId.HasValue) throw Invalid();
            if (state.MatchId != e.MatchId) { m.CurrentProgress = 0; state = new() { MatchId = e.MatchId }; }
        }
        if (r.ResetEventType != null && Parse<MissionEventType>(r.ResetEventType) == type)
        { m.CurrentProgress = 0; state.LastValue = null; state.UniqueValues.Clear(); }
        else
        {
            var matches = r.Conditions.All(x => e.EventData.TryGetValue(x.Key, out var actual) && Equal(x.Value, actual));
            if (!matches)
            {
                if (progressType != MissionProgressType.Streak) return false;
                m.CurrentProgress = 0; state.LastValue = null;
            }
            else
            {
                switch (progressType)
                {
                    case MissionProgressType.Count: m.CurrentProgress = Math.Min(m.TargetProgress, checked(m.CurrentProgress + 1)); break;
                    case MissionProgressType.Boolean: m.CurrentProgress = m.TargetProgress; break;
                    case MissionProgressType.MaxValue:
                        if (!e.EventData.TryGetValue(r.ProgressField, out var number) || number.ValueKind != JsonValueKind.Number || !number.TryGetInt32(out var value) || value < 0) throw Invalid();
                        m.CurrentProgress = Math.Min(m.TargetProgress, Math.Max(m.CurrentProgress, value)); break;
                    case MissionProgressType.Streak:
                        if (r.RepeatValue)
                        {
                            if (!e.EventData.TryGetValue(r.ProgressField, out var current) || current.ValueKind == JsonValueKind.Null) throw Invalid();
                            var key = Canonical(new() { ["key"] = current });
                            m.CurrentProgress = state.LastValue == key ? Math.Min(m.TargetProgress, checked(m.CurrentProgress + 1)) : 1;
                            state.LastValue = key;
                        }
                        else m.CurrentProgress = Math.Min(m.TargetProgress, checked(m.CurrentProgress + 1));
                        break;
                    case MissionProgressType.UniqueCount:
                        if (!e.EventData.TryGetValue(r.ProgressField, out var unique) || unique.ValueKind == JsonValueKind.Null) throw Invalid();
                        state.UniqueValues.Add(Canonical(new() { ["key"] = unique }));
                        m.CurrentProgress = Math.Min(m.TargetProgress, state.UniqueValues.Count); break;
                }
            }
        }
        m.LastEventAt = Stamp(e.OccurredAt); m.ProgressStateJson = Json(state);
        m.Status = m.CurrentProgress >= m.TargetProgress ? PlayerMissionStatus.Completed : m.CurrentProgress > 0 ? PlayerMissionStatus.InProgress : PlayerMissionStatus.Assigned;
        if (m.Status == PlayerMissionStatus.Completed) m.CompletedAt = Stamp(e.OccurredAt);
        return old != m.CurrentProgress;
    }
    private static bool Equal(JsonElement a, JsonElement b) =>
        a.ValueKind == JsonValueKind.Number && b.ValueKind == JsonValueKind.Number ? a.GetDecimal() == b.GetDecimal() : a.ValueKind == b.ValueKind && a.ToString() == b.ToString();
}
