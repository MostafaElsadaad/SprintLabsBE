using System.Data;
using System.Data.Common;
using System.Net;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses.Shop;

namespace Infrastructure.Services;

// Owns the transaction boundary shared by the wallet, permanent ownership and purchase receipt.
public partial class ShopService(ApplicationDbContext context) : IShopService
{
    private async Task<Player> PlayerAsync(long userId, CancellationToken ct)
    {
        if (userId <= 0) throw Error(HttpStatusCode.Unauthorized, "Authentication required.");
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user == null) throw Missing();
        if (user.Status != UserStatus.Active || user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
            throw Error(HttpStatusCode.Forbidden, "Account unavailable.");
        return await context.Players.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? throw Missing();
    }

    private async Task AdminAsync(long userId, CancellationToken ct)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user == null || user.Status != UserStatus.Active || !user.IsPlatformAdmin ||
            user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
            throw Error(HttpStatusCode.Forbidden, "Platform administrator required.");
    }

    private async Task<T> AtomicAsync<T>(long userId, Func<Player, Task<T>> action, CancellationToken ct)
    {
        // Resolve identity before locking; refresh the tracked player after acquiring the lock so
        // an earlier read in this request cannot overwrite a concurrent match/mission wallet update.
        var id = (await PlayerAsync(userId, ct)).Id;
        try
        {
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            Player player;
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
            {
                var locked = (await context.Players.FromSqlInterpolated($"SELECT * FROM Players WHERE Id = {id} FOR UPDATE")
                    .AsNoTracking().ToListAsync(ct)).SingleOrDefault() ?? throw Missing();
                var tracked = context.Players.Local.SingleOrDefault(x => x.Id == id);
                if (tracked != null) context.Entry(tracked).State = EntityState.Detached;
                context.Attach(locked);
                player = locked;
            }
            else
            {
                player = await context.Players.SingleAsync(x => x.Id == id, ct);
                await context.Entry(player).ReloadAsync(ct);
            }
            if (player.UserId != userId) throw Missing();
            var result = await action(player);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException) { context.ChangeTracker.Clear(); throw Unavailable(); }
        catch (DbException) { context.ChangeTracker.Clear(); throw Unavailable(); }
        catch (OverflowException) { context.ChangeTracker.Clear(); throw Conflict("Inventory limit reached."); }
        catch { context.ChangeTracker.Clear(); throw; }
    }

    private static ShopItemResponse ItemView(Item item, int quantity, long? equipped) => new()
    {
        ItemId = item.Id, ItemKey = item.ItemKey, Name = item.Name, IconKey = item.IconKey,
        PrefabKey = item.PrefabKey, IsCharacter = item.IsCharacter, QuantityOwned = quantity,
        IsEquipped = equipped == item.Id && quantity > 0
    };
    private static ShopPurchaseResponse Receipt(ShopPurchase purchase, bool replay = false) => new()
    {
        PurchaseId = purchase.Id, ProductId = purchase.ShopProductId, ItemId = purchase.ItemId,
        RequestId = Guid.ParseExact(purchase.RequestId, "N"), CoinsSpent = purchase.CoinsSpent,
        CoinsAfter = purchase.CoinsAfter, PurchasedAt = purchase.PurchasedAt, IsReplay = replay
    };
    private static GenericException Error(HttpStatusCode status, string message) => new(ErrorCode.Failure, message, status);
    private static GenericException Invalid() => Error(HttpStatusCode.BadRequest, ErrorMessage.InvalidInput);
    private static GenericException Missing() => Error(HttpStatusCode.NotFound, ErrorMessage.NotFound);
    private static GenericException Conflict(string message) => Error(HttpStatusCode.Conflict, message);
    private static GenericException Unavailable() => Error(HttpStatusCode.ServiceUnavailable, "Shop temporarily unavailable; retry with the same requestId.");
}
