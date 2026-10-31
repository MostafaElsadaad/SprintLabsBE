using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    public async Task<ShopCatalogueResponse> CatalogueAsync(long userId, CancellationToken ct)
    {
        var player = await PlayerAsync(userId, ct);
        return new() { Coins = player.Gold, Boxes = await BoxesForAsync(player.Id, false, ct) };
    }
    public async Task<ShopInventoryResponse> InventoryAsync(long userId, CancellationToken ct)
        => await InventoryForAsync(await PlayerAsync(userId, ct), ct);

    private async Task<List<ShopBoxResponse>> BoxesForAsync(long playerId, bool ownedOnly, CancellationToken ct)
    {
        var quantities = await context.Set<PlayerBox>().AsNoTracking().Where(x => x.PlayerProfileId == playerId).ToDictionaryAsync(x => x.ShopProductId, x => x.Quantity, ct);
        var products = await context.ShopProducts.AsNoTracking().Where(x => x.BoxKey != null).OrderBy(x => x.Id).ToListAsync(ct);
        return products.Where(x => ownedOnly ? quantities.GetValueOrDefault(x.Id) > 0 : x.IsActive)
            .Select(x => BoxView(x, quantities.GetValueOrDefault(x.Id))).ToList();
    }
    private async Task<ShopInventoryResponse> InventoryForAsync(Player player, CancellationToken ct)
    {
        var equipment = await context.Set<PlayerCosmeticEquipment>().AsNoTracking().Where(x => x.PlayerProfileId == player.Id).OrderBy(x=>x.Category).ToListAsync(ct);
        var items = await (from owned in context.PlayerInventoryItems.AsNoTracking()
            join item in context.Items.AsNoTracking() on owned.ItemId equals item.Id
            where owned.PlayerProfileId == player.Id && owned.Quantity > 0
            orderby item.Id select new { Item = item, owned.Quantity }).ToListAsync(ct);
        return new() { Coins = player.Gold, EquippedCharacterItemId = player.EquippedCharacterItemId,
            Boxes = await BoxesForAsync(player.Id, true, ct),
            EquippedCosmetics = equipment.Select(x=>new ShopEquippedCosmeticResponse {Category=x.Category,ItemId=x.ItemId}).ToList(),
            Items = items.Select(x => ItemView(x.Item, x.Quantity, x.Item.IsCharacter ? player.EquippedCharacterItemId : equipment.FirstOrDefault(e=>e.ItemId==x.Item.Id)?.ItemId)).ToList() };
    }
    public async Task<List<ShopPurchaseResponse>> HistoryAsync(long userId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || page > 1000000 || pageSize < 1 || pageSize > 100) throw Invalid();
        var player = await PlayerAsync(userId, ct);
        var purchases = await context.Set<ShopPurchase>().AsNoTracking().Where(x => x.PlayerProfileId == player.Id)
            .OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return purchases.Select(x => Receipt(x)).ToList();
    }
}
