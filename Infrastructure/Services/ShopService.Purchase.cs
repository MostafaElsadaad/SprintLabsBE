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
            if (await context.Set<ShopOperation>().AnyAsync(x => x.PlayerProfileId == player.Id && x.RequestId == key, ct))
                throw Conflict("requestId was already used for another action.");
            throw Conflict("Direct character purchases have been replaced by magical boxes.");
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
