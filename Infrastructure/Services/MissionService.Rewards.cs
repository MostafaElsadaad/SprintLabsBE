using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using Shared.Responses;
using System.Security.Cryptography;
namespace Infrastructure.Services;
public partial class MissionService
{
    public async Task<List<MissionClaimResponse>> ClaimAsync(long userId, long? missionId, CancellationToken ct)
    {
        if (missionId is <= 0) throw Invalid();
        var id = await MyPlayerAsync(userId, ct);
        return await AtomicAsync(new[] { id }, async players => {
            var player = players[id]; var now = DateTime.UtcNow;
            // Ownership checked before expiry processing; an unknown mission must not trigger any changes.
            if (missionId.HasValue && !await _context.PlayerMissions.AnyAsync(x => x.Id == missionId && x.PlayerProfileId == id, ct)) throw Missing();
            await ExpireAsync(player, now, ct);
            await _context.SaveChangesAsync(ct);
            var missions = await _context.PlayerMissions.Where(x => x.PlayerProfileId == id &&
                (missionId.HasValue ? x.Id == missionId : x.Status == PlayerMissionStatus.Completed)).OrderBy(x => x.Id).ToListAsync(ct);
            var results = new List<MissionClaimResponse>();
            foreach (var m in missions)
            {
                if (m.Status is PlayerMissionStatus.Claimed or PlayerMissionStatus.AutoClaimed)
                    results.Add(Read<MissionClaimResponse>(m.ClaimSnapshotJson!));
                else if (m.Status == PlayerMissionStatus.Completed) results.Add(await GrantAsync(player, m, false, now, ct));
                else throw Conflict();
            }
            return results;
        }, ct);
    }
    private async Task<MissionClaimResponse> GrantAsync(Player p, PlayerMission m, bool auto, DateTime now, CancellationToken ct)
    {
        if (p.Experience < 0 || p.Gold < 0) throw Conflict();
        var snapshot = Read<MissionDefinitionSnapshot>(m.DefinitionSnapshotJson);
        var result = new MissionClaimResponse { PlayerMissionId = m.Id, Status = (auto ? PlayerMissionStatus.AutoClaimed : PlayerMissionStatus.Claimed).ToString() };
        foreach (var reward in snapshot.Rules.Rewards)
        {
            var type = Parse<MissionRewardType>(reward.RewardType);
            var response = new MissionRewardResponse { RewardType = type.ToString(), Amount = reward.Amount, ShopProductId = reward.ShopProductId };
            long? itemId = null; var amount = reward.Amount ?? 0;
            switch (type)
            {
                case MissionRewardType.XP:
                    if (amount < 1 || p.Experience > int.MaxValue - amount) throw Conflict();
                    var level = _levels.CalculateProgression(new LevelProgressionRequest { OldTotalXp = p.Experience, XpGained = amount });
                    p.Experience = level.NewTotalXp; p.Level = level.NewLevel;
                    _context.PlayerXpLogs.Add(new PlayerXpLog { PlayerProfileId = p.Id, SourceType = XpSourceType.Mission,
                        SourceId = m.Id, BaseXp = amount, Multiplier = 1, FinalXp = amount, CreatedAt = now }); break;
                case MissionRewardType.Coins: p.Gold = checked(p.Gold + amount); break;
                case MissionRewardType.Box:
                    var box = snapshot.Boxes.Single(x => x.ShopProductId == reward.ShopProductId);
                    var roll = RandomNumberGenerator.GetInt32(box.Items.Sum(x => x.Weight));
                    var item = box.Items[0]; foreach (var candidate in box.Items) { if (roll < candidate.Weight) { item = candidate; break; } roll -= candidate.Weight; }
                    var owned = _context.PlayerInventoryItems.Local.SingleOrDefault(x => x.PlayerProfileId == p.Id && x.ItemId == item.ItemId)
                        ?? await _context.PlayerInventoryItems.SingleOrDefaultAsync(x => x.PlayerProfileId == p.Id && x.ItemId == item.ItemId, ct);
                    if (owned == null) { owned = new() { PlayerProfileId = p.Id, ItemId = item.ItemId }; _context.PlayerInventoryItems.Add(owned); }
                    owned.Quantity = checked(owned.Quantity + item.Quantity); itemId = item.ItemId; amount = item.Quantity;
                    response.BoxName = box.Name; response.RewardItem = Read<MissionItemResponse>(Json(item)); response.RewardItem.QuantityOwned = owned.Quantity;
                    break;
            }
            _context.MissionClaimLogs.Add(new MissionClaimLog { PlayerMissionId = m.Id, PlayerProfileId = p.Id,
                MissionTemplateId = m.MissionTemplateId, RewardType = type, Amount = amount, ShopProductId = reward.ShopProductId, RewardItemId = itemId, CreatedAt = now });
            result.Rewards.Add(response);
        }
        p.UpdatedAt = now; m.Status = auto ? PlayerMissionStatus.AutoClaimed : PlayerMissionStatus.Claimed; m.ClaimedAt = now;
        result.NewXp = p.Experience; result.NewLevel = p.Level; result.NewCoins = p.Gold;
        m.ClaimSnapshotJson = Json(result); return result;
    }
    private async Task<(int expired, int claimed)> ExpireAsync(Player p, DateTime now, CancellationToken ct)
    {
        var missions = await _context.PlayerMissions.Where(x => x.PlayerProfileId == p.Id &&
            (x.Status == PlayerMissionStatus.Assigned || x.Status == PlayerMissionStatus.InProgress || x.Status == PlayerMissionStatus.Completed) &&
            _context.MissionActivations.Any(a => a.Id == x.MissionActivationId &&
                (a.Status == MissionActivationStatus.Cancelled || a.Status == MissionActivationStatus.Ended || (a.EndsAt != null && a.EndsAt <= now))))
            .OrderBy(x => x.Id).ToListAsync(ct);
        var expired = 0; var claimed = 0;
        foreach (var m in missions)
        {
            var activation = await _context.MissionActivations.SingleAsync(x => x.Id == m.MissionActivationId, ct);
            if (m.Status == PlayerMissionStatus.Completed && activation.Status != MissionActivationStatus.Cancelled && activation.AutoClaimCompletedOnReset)
            { await GrantAsync(p, m, true, now, ct); claimed++; }
            else { m.Status = PlayerMissionStatus.Expired; expired++; }
        }
        return (expired, claimed);
    }
    public async Task<MissionResetResponse> ResetAsync(long adminUserId, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct); var now = DateTime.UtcNow; var result = new MissionResetResponse();
        var activationIds = await _context.MissionActivations.Where(x => x.Status == MissionActivationStatus.Active && x.EndsAt != null && x.EndsAt <= now).Select(x => x.Id).ToListAsync(ct);
        var playerIds = await _context.PlayerMissions.Where(x => activationIds.Contains(x.MissionActivationId) &&
            (x.Status == PlayerMissionStatus.Assigned || x.Status == PlayerMissionStatus.InProgress || x.Status == PlayerMissionStatus.Completed)).Select(x => x.PlayerProfileId).Distinct().OrderBy(x => x).ToListAsync(ct);
        // Each profile settles independently. If one fails, retry resumes the remaining profiles.
        foreach (var id in playerIds)
        {
            // Reset must settle earned rewards for suspended players too; unlike interactive writes it is maintenance.
            var totals = await ResetPlayerAsync(id, now, ct);
            result.ExpiredMissions += totals.expired; result.AutoClaimedMissions += totals.claimed;
        }
        foreach (var id in activationIds)
        {
            var a = await _context.MissionActivations.SingleAsync(x => x.Id == id, ct);
            if (a.Status == MissionActivationStatus.Active) { a.Status = MissionActivationStatus.Ended; result.EndedActivations++; }
        }
        try { await _context.SaveChangesAsync(ct); } catch (DbUpdateException) { throw Unavailable(); }
        return result;
    }
    private async Task<(int expired, int claimed)> ResetPlayerAsync(long id, DateTime now, CancellationToken ct)
    {
        // Active profiles use the same serialization path. Suspended accounts retain earned rewards;
        // the maintenance variant allows only the already persisted profile, never arbitrary client identity.
        try
        {
            await using var tx = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
            var p = _context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true
                ? (await _context.Players.FromSqlInterpolated($"SELECT * FROM Players WHERE Id = {id} FOR UPDATE").ToListAsync(ct)).Single()
                : await _context.Players.SingleAsync(x => x.Id == id, ct);
            var result = await ExpireAsync(p, now, ct); await _context.SaveChangesAsync(ct);
            if (tx != null) await tx.CommitAsync(ct); return result;
        }
        catch (DbUpdateException) { throw Unavailable(); } catch (System.Data.Common.DbException) { throw Unavailable(); }
        catch (OverflowException) { throw Conflict(); }
    }
}
