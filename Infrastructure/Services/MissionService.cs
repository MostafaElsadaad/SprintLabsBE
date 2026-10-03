using System.Data;
using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests;
using Shared.Responses;
namespace Infrastructure.Services;

public partial class MissionService : IMissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILevelProgressionService _levels;
    public MissionService(ApplicationDbContext context, ILevelProgressionService levels)
    { _context = context; _levels = levels; }

    private async Task<T> AtomicAsync<T>(IEnumerable<long> ids, Func<Dictionary<long, Player>, Task<T>> action, CancellationToken ct, IEnumerable<long>? matchIds = null)
    {
        try
        {
            await using var tx = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
            // Same lock order as match completion: exact matches first, then ascending profiles.
            if (_context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true && matchIds != null)
                foreach (var matchId in matchIds.Distinct().Order())
                    await _context.Matches.FromSqlInterpolated($"SELECT * FROM Matches WHERE Id = {matchId} FOR UPDATE").ToListAsync(ct);
            var players = new Dictionary<long, Player>();
            foreach (var id in ids.Distinct().Order())
            {
                var player = _context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true
                    ? (await _context.Players.FromSqlInterpolated($"SELECT * FROM Players WHERE Id = {id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()
                    : await _context.Players.SingleOrDefaultAsync(x => x.Id == id, ct);
                if (player == null || !player.UserId.HasValue || !await _context.Users.AnyAsync(x => x.Id == player.UserId && x.Status == UserStatus.Active, ct)) throw Missing();
                players.Add(id, player);
            }
            var result = await action(players);
            await _context.SaveChangesAsync(ct);
            if (tx != null) await tx.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException) { throw Unavailable(); }
        catch (DbException) { throw Unavailable(); }
        catch (OverflowException) { throw Conflict(); }
    }
    private async Task<long> MyPlayerAsync(long userId, CancellationToken ct)
    {
        var id = await _context.Players.Where(p => p.UserId == userId &&
            _context.Users.Any(u => u.Id == userId && u.Status == UserStatus.Active)).Select(p => (long?)p.Id).SingleOrDefaultAsync(ct);
        return id ?? throw Missing();
    }
    private async Task AdminAsync(long userId, CancellationToken ct)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active && x.IsPlatformAdmin, ct))
            throw new GenericException(ErrorCode.Failure, ErrorMessage.GeneralError, HttpStatusCode.Forbidden);
    }
    private static T Parse<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) || int.TryParse(value, out _) || !Enum.TryParse<T>(value, true, out var result) || !Enum.IsDefined(result)) throw Invalid();
        return result;
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json) ?? throw Conflict();
    private static DateTime Stamp(DateTime value) => new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);
    private static string Canonical(Dictionary<string, JsonElement> data)
    {
        ValidateData(data);
        var values = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in data) values[key] = value.ValueKind switch
        { JsonValueKind.String => value.GetString(), JsonValueKind.Number => value.GetDecimal(),
                JsonValueKind.True => true, JsonValueKind.False => false, _ => null };
        return Json(values);
    }
    private static void ValidateData(Dictionary<string, JsonElement>? data)
    {
        if (data == null || data.Count > 16 || data.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 64 ||
            x.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Undefined ||
            (x.Value.ValueKind == JsonValueKind.String && x.Value.GetString()!.Length > 200) ||
            (x.Value.ValueKind == JsonValueKind.Number && !x.Value.TryGetDecimal(out _)))) throw Invalid();
    }
    private static GenericException Invalid() => new(ErrorCode.Failure, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
    private static GenericException Missing() => new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
    private static GenericException Conflict() => new(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
    private static GenericException Unavailable() => new(ErrorCode.Failure, ErrorMessage.GeneralError, HttpStatusCode.ServiceUnavailable);
    private static PlayerMissionResponse Map(PlayerMission mission)
    {
        var r = Read<MissionDefinitionSnapshot>(mission.DefinitionSnapshotJson).Rules;
        return new() { PlayerMissionId = mission.Id, MissionKey = r.MissionKey, Title = r.Title, Description = r.Description,
            Category = r.Category, PeriodType = r.PeriodType, CurrentProgress = mission.CurrentProgress,
            TargetProgress = mission.TargetProgress, Status = mission.Status.ToString(), ExpiresAt = mission.ExpiresAt,
            Rewards = r.Rewards.Select(x => new MissionRewardResponse { RewardType = x.RewardType, Amount = x.Amount, ShopProductId = x.ShopProductId }).ToList() };
    }
}
