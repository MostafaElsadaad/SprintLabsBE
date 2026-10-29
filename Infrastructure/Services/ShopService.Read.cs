using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    public async Task<ShopCatalogueResponse> CatalogueAsync(long userId, CancellationToken ct)
    {
        var player = await PlayerAsync(userId, ct);
        var products = await (from p in context.ShopProducts.AsNoTracking()
            join i in context.Items.AsNoTracking() on p.ItemId equals (long?)i.Id
            where p.IsActive && p.CoinPrice > 0 && i.IsCharacter
            orderby p.Id
            select new { Product = p, Item = i }).ToListAsync(ct);
        var owned = await context.PlayerInventoryItems.AsNoTracking().Where(x => x.PlayerProfileId == player.Id)
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity, ct);
        return new() { Coins = player.Gold, Products = products.Select(x => new ShopProductResponse
        {
            ProductId = x.Product.Id, CoinPrice = x.Product.CoinPrice!.Value, IsActive = x.Product.IsActive,
            Item = ItemView(x.Item, owned.GetValueOrDefault(x.Item.Id), player.EquippedCharacterItemId)
        }).ToList() };
    }

    public async Task<ShopInventoryResponse> InventoryAsync(long userId, CancellationToken ct)
        => await InventoryForAsync(await PlayerAsync(userId, ct), ct);

    private async Task<ShopInventoryResponse> InventoryForAsync(Player player, CancellationToken ct)
    {
        var items = await (from owned in context.PlayerInventoryItems.AsNoTracking()
            join item in context.Items.AsNoTracking() on owned.ItemId equals item.Id
            where owned.PlayerProfileId == player.Id && owned.Quantity > 0
            orderby item.Id select new { Item = item, owned.Quantity }).ToListAsync(ct);
        return new() { Coins = player.Gold, EquippedCharacterItemId = player.EquippedCharacterItemId,
            Items = items.Select(x => ItemView(x.Item, x.Quantity, player.EquippedCharacterItemId)).ToList() };
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
