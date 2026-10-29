using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    public async Task<ShopPurchaseResponse> PurchaseAsync(long userId, long productId, Guid requestId, CancellationToken ct)
    {
        if (productId <= 0 || requestId == Guid.Empty) throw Invalid();
        return await AtomicAsync(userId, async player =>
        {
            var key = requestId.ToString("N");
            var previous = await context.Set<ShopPurchase>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.PlayerProfileId == player.Id && x.RequestId == key, ct);
            if (previous != null)
            {
                if (previous.ShopProductId != productId) throw Conflict("requestId was already used for another product.");
                return Receipt(previous, true);
            }
            var product = await context.ShopProducts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId, ct);
            if (product == null || !product.IsActive || product.CoinPrice is not > 0 || product.ItemId == null) throw Missing();
            var item = await context.Items.AsNoTracking().SingleOrDefaultAsync(x => x.Id == product.ItemId && x.IsCharacter, ct);
            if (item == null) throw Missing();
            var owned = await context.PlayerInventoryItems.SingleOrDefaultAsync(x => x.PlayerProfileId == player.Id && x.ItemId == item.Id, ct);
            if (owned?.Quantity > 0) throw Conflict("Item already owned.");
            if (player.Gold < product.CoinPrice.Value) throw Conflict("Insufficient coins.");
            // The client supplies intent only: price, balance, item and ownership come from the database.
            player.Gold -= product.CoinPrice.Value;
            var now = DateTime.UtcNow;
            player.UpdatedAt = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);
            if (owned == null)
            {
                owned = new PlayerInventoryItem { PlayerProfileId = player.Id, ItemId = item.Id };
                context.PlayerInventoryItems.Add(owned);
            }
            owned.Quantity = 1;
            var purchase = new ShopPurchase { PlayerProfileId = player.Id, ShopProductId = productId,
                ItemId = item.Id, RequestId = key, CoinsSpent = product.CoinPrice.Value,
                CoinsAfter = player.Gold, PurchasedAt = player.UpdatedAt.Value };
            context.Set<ShopPurchase>().Add(purchase);
            await context.SaveChangesAsync(ct);
            return Receipt(purchase);
        }, ct);
    }

    public async Task<ShopInventoryResponse> EquipAsync(long userId, long? itemId, CancellationToken ct)
    {
        if (itemId is <= 0) throw Invalid();
        return await AtomicAsync(userId, async player =>
        {
            if (itemId.HasValue && !await (from owned in context.PlayerInventoryItems
                join item in context.Items on owned.ItemId equals item.Id
                where owned.PlayerProfileId == player.Id && owned.ItemId == itemId && owned.Quantity > 0 && item.IsCharacter
                select owned.Id).AnyAsync(ct)) throw Conflict("Character is not owned.");
            player.EquippedCharacterItemId = itemId;
            player.UpdatedAt = DateTime.UtcNow;
            // Save first so the response query sees the selected item. No coins or inventory are consumed.
            await context.SaveChangesAsync(ct);
            return await InventoryForAsync(player, ct);
        }, ct);
    }
}
