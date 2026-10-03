using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using Shared.Responses;
namespace Infrastructure.Services;
public partial class MissionService
{
    public async Task<long> CreateTemplateAsync(long adminUserId, MissionTemplateRequest request, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        return await AtomicAsync(Array.Empty<long>(), async _ => { var t = await AddTemplateAsync(request, ct); await _context.SaveChangesAsync(ct); return t.Id; }, ct);
    }
    private async Task<MissionTemplate> AddTemplateAsync(MissionTemplateRequest r, CancellationToken ct)
    {
        if (r == null || string.IsNullOrWhiteSpace(r.MissionKey) || r.MissionKey.Length > 100 || r.MissionKey.Trim() != r.MissionKey ||
            string.IsNullOrWhiteSpace(r.Title) || r.Title.Length > 200 || r.Description == null || r.Description.Length > 2000 ||
            r.TargetValue is < 1 or > 10000 || string.IsNullOrWhiteSpace(r.ProgressField) || r.ProgressField.Length > 64 ||
            r.Rewards == null || r.Rewards.Count is < 1 or > 10 || r.Rewards.Any(x => x == null)) throw Invalid();
        var progress = Parse<MissionProgressType>(r.ProgressType); var evt = Parse<MissionEventType>(r.EventType);
        var reset = r.ResetEventType == null ? (MissionEventType?)null : Parse<MissionEventType>(r.ResetEventType);
        if (reset == evt || (reset.HasValue && progress != MissionProgressType.Streak) || (r.RepeatValue && progress != MissionProgressType.Streak) ||
            (progress == MissionProgressType.Boolean && r.TargetValue != 1)) throw Invalid();
        ValidateData(r.Conditions);
        if (await _context.MissionTemplates.AnyAsync(x => x.MissionKey == r.MissionKey, ct)) throw Conflict();
        var t = new MissionTemplate { MissionKey = r.MissionKey, Title = r.Title, Description = r.Description,
            Category = Parse<MissionCategory>(r.Category), PeriodType = Parse<MissionPeriodType>(r.PeriodType), EventType = evt,
            ProgressType = progress, TargetValue = r.TargetValue, ConditionsJson = Canonical(r.Conditions), ProgressField = r.ProgressField,
            ResetEventType = reset, MatchScoped = r.MatchScoped, RepeatValue = r.RepeatValue, CreatedAt = DateTime.UtcNow };
        foreach (var reward in r.Rewards)
        {
            var type = Parse<MissionRewardType>(reward.RewardType);
            if (type == MissionRewardType.Box)
            {
                if (reward.Amount != null || reward.ShopProductId is null or <= 0 || !await _context.ShopProducts.AnyAsync(x => x.Id == reward.ShopProductId && x.IsActive, ct)) throw Invalid();
            }
            else if (reward.Amount is null or < 1 or > 1000000 || reward.ShopProductId != null) throw Invalid();
            t.Rewards.Add(new MissionReward { RewardType = type, Amount = reward.Amount, ShopProductId = reward.ShopProductId });
        }
        // Validate configured boxes now; assignment will snapshot their contents again.
        await SnapshotAsync(t, ct);
        _context.MissionTemplates.Add(t); return t;
    }
    public async Task<long> CreateActivationAsync(long adminUserId, MissionActivationRequest request, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        return await AtomicAsync(Array.Empty<long>(), async _ => { var a = await AddActivationAsync(request, ct); await _context.SaveChangesAsync(ct); return a.Id; }, ct);
    }
    private async Task<MissionActivation> AddActivationAsync(MissionActivationRequest r, CancellationToken ct)
    {
        if (r == null || string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 200 || r.StartsAt.Kind != DateTimeKind.Utc ||
            r.Items == null || r.Items.Count is < 1 or > 100 || r.Items.Any(x => x == null) ||
            r.RandomMissionCount is < 0 or > 100 || r.Items.Select(x => x.MissionTemplateId).Distinct().Count() != r.Items.Count) throw Invalid();
        var period = Parse<MissionPeriodType>(r.PeriodType);
        if ((period != MissionPeriodType.Open && r.EndsAt == null) || (period == MissionPeriodType.Open && r.EndsAt != null) ||
            (r.EndsAt.HasValue && (r.EndsAt.Value.Kind != DateTimeKind.Utc || r.EndsAt <= r.StartsAt)) ||
            (r.EndsAt.HasValue && r.EndsAt <= DateTime.UtcNow)) throw Invalid();
        var ids = r.Items.Select(x => x.MissionTemplateId).ToList();
        if (await _context.MissionTemplates.CountAsync(x => ids.Contains(x.Id) && x.IsActive && x.PeriodType == period, ct) != ids.Count) throw Invalid();
        if (await _context.MissionActivations.AnyAsync(x => x.Name == r.Name, ct)) throw Conflict();
        var a = new MissionActivation { Name = r.Name, PeriodType = period, StartsAt = Stamp(r.StartsAt), EndsAt = r.EndsAt.HasValue ? Stamp(r.EndsAt.Value) : null,
            RandomMissionCount = r.RandomMissionCount, AutoClaimCompletedOnReset = r.AutoClaimCompletedOnReset, CreatedAt = DateTime.UtcNow };
        foreach (var item in r.Items)
        {
            var mode = Parse<MissionAssignmentMode>(item.AssignmentMode);
            if (item.Weight is < 1 or > 100000) throw Invalid();
            a.Items.Add(new MissionActivationItem { MissionTemplateId = item.MissionTemplateId, AssignmentMode = mode, Weight = item.Weight, SortOrder = item.SortOrder });
        }
        if (a.Items.Count(x => x.AssignmentMode == MissionAssignmentMode.RandomPool) < a.RandomMissionCount) throw Invalid();
        _context.MissionActivations.Add(a); return a;
    }
    public async Task<List<MissionTemplateResponse>> PreviewTemplatesAsync(long adminUserId, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        var templates = await _context.MissionTemplates.Include(x => x.Rewards).OrderBy(x => x.Id).Take(100).ToListAsync(ct);
        var results = new List<MissionTemplateResponse>();
        foreach (var t in templates) results.Add(new MissionTemplateResponse { MissionTemplateId = t.Id, IsActive = t.IsActive, Definition = (await SnapshotAsync(t, ct)).Rules });
        return results;
    }
}
