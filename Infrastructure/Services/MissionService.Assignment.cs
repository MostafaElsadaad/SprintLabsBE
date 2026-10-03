using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using Shared.Responses;
using System.Security.Cryptography;
using System.Text.Json;
namespace Infrastructure.Services;
public partial class MissionService
{
    public async Task<List<PlayerMissionResponse>> GetMyAsync(long userId, CancellationToken ct)
    {
        var id = await MyPlayerAsync(userId, ct);
        return await AtomicAsync(new[] { id }, async players => {
            var now = DateTime.UtcNow;
            await ExpireAsync(players[id], now, ct);
            await AssignAsync(id, now, ct);
            await _context.SaveChangesAsync(ct);
            return (await _context.PlayerMissions.Where(x => x.PlayerProfileId == id && x.Status != PlayerMissionStatus.Expired &&
                _context.MissionActivations.Any(a => a.Id == x.MissionActivationId && a.Status == MissionActivationStatus.Active && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt > now)))
                .OrderBy(x => x.MissionActivationId).ThenBy(x => x.Id).ToListAsync(ct)).Select(Map).ToList();
        }, ct);
    }
    private async Task AssignAsync(long playerId, DateTime now, CancellationToken ct)
    {
        var activations = await _context.MissionActivations.Include(x => x.Items).Where(x =>
            x.Status == MissionActivationStatus.Active && x.StartsAt <= now && (x.EndsAt == null || x.EndsAt > now)).OrderBy(x => x.Id).ToListAsync(ct);
        foreach (var a in activations)
        {
            if (await _context.PlayerMissionAssignments.AnyAsync(x => x.PlayerProfileId == playerId && x.MissionActivationId == a.Id, ct) ||
                _context.PlayerMissionAssignments.Local.Any(x => x.PlayerProfileId == playerId && x.MissionActivationId == a.Id)) continue;
            var ids = a.Items.Select(x => x.MissionTemplateId).ToList();
            var templates = await _context.MissionTemplates.Include(x => x.Rewards).Where(x => ids.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
            var selected = a.Items.Where(x => x.AssignmentMode == MissionAssignmentMode.Fixed && templates.ContainsKey(x.MissionTemplateId)).ToList();
            var pool = a.Items.Where(x => x.AssignmentMode == MissionAssignmentMode.RandomPool && templates.ContainsKey(x.MissionTemplateId)).ToList();
            if (pool.Count < a.RandomMissionCount) throw Conflict();
            for (var i = 0; i < a.RandomMissionCount; i++)
            {
                var roll = RandomNumberGenerator.GetInt32(pool.Sum(x => x.Weight));
                var chosen = pool[0];
                foreach (var item in pool) { if (roll < item.Weight) { chosen = item; break; } roll -= item.Weight; }
                selected.Add(chosen); pool.Remove(chosen);
            }
            foreach (var item in selected.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
            {
                var template = templates[item.MissionTemplateId];
                var snapshot = await SnapshotAsync(template, ct);
                _context.PlayerMissions.Add(new PlayerMission { PlayerProfileId = playerId, MissionActivationId = a.Id,
                    MissionTemplateId = template.Id, TargetProgress = template.TargetValue, AssignedAt = now, ExpiresAt = a.EndsAt,
                    DefinitionSnapshotJson = Json(snapshot) });
            }
            _context.PlayerMissionAssignments.Add(new PlayerMissionAssignment { PlayerProfileId = playerId, MissionActivationId = a.Id, AssignedAt = now });
        }
    }
    private async Task<MissionDefinitionSnapshot> SnapshotAsync(MissionTemplate t, CancellationToken ct)
    {
        var snapshot = new MissionDefinitionSnapshot { Rules = new MissionTemplateRequest {
            MissionKey = t.MissionKey, Title = t.Title, Description = t.Description, Category = t.Category.ToString(), PeriodType = t.PeriodType.ToString(),
            EventType = t.EventType.ToString(), ProgressType = t.ProgressType.ToString(), TargetValue = t.TargetValue,
            Conditions = Read<Dictionary<string, JsonElement>>(t.ConditionsJson), ProgressField = t.ProgressField,
            ResetEventType = t.ResetEventType?.ToString(), MatchScoped = t.MatchScoped, RepeatValue = t.RepeatValue,
            Rewards = t.Rewards.OrderBy(x => x.Id).Select(x => new MissionRewardRequest { RewardType = x.RewardType.ToString(), Amount = x.Amount, ShopProductId = x.ShopProductId }).ToList() } };
        foreach (var id in t.Rewards.Where(x => x.RewardType == MissionRewardType.Box).Select(x => x.ShopProductId!.Value).Distinct())
        {
            var box = await _context.ShopProducts.Include(x => x.BoxRewards).SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
            if (box == null || box.BoxRewards.Count == 0 || box.BoxRewards.Count > 100 || box.BoxRewards.Any(x => x.Weight is < 1 or > 100000 || x.Quantity is < 1 or > 10000)) throw Conflict();
            var itemIds = box.BoxRewards.Select(x => x.ItemId).ToList();
            var items = await _context.Items.Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            if (items.Count != itemIds.Distinct().Count()) throw Conflict();
            snapshot.Boxes.Add(new MissionBoxSnapshot { ShopProductId = id, Name = box.Name, Items = box.BoxRewards.OrderBy(x => x.Id).Select(x => {
                var item = items[x.ItemId]; return new MissionItemResponse { ItemId = item.Id, ItemKey = item.ItemKey,
                    Name = item.Name, Rarity = item.Rarity, Quantity = x.Quantity, Weight = x.Weight, IconKey = item.IconKey, PrefabKey = item.PrefabKey };
            }).ToList() });
        }
        return snapshot;
    }
}
