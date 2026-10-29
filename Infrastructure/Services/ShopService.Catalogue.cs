using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests.Shop;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    public async Task<ShopProductResponse> UpsertProductAsync(long adminUserId, string itemKey, ShopProductRequest request, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        itemKey = itemKey.Trim();
        var name = request.Name?.Trim(); var icon = request.IconKey?.Trim(); var prefab = request.PrefabKey?.Trim();
        if (itemKey.Length is < 1 or > 100 || itemKey.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-')) ||
            string.IsNullOrWhiteSpace(name) || name.Length > 200 || name.Any(char.IsControl) || request.CoinPrice < 1 ||
            string.IsNullOrWhiteSpace(icon) || icon.Length > 200 || icon.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(prefab) || prefab.Length > 200 || prefab.Any(char.IsControl)) throw Invalid();
        try
        {
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            var item = await context.Items.SingleOrDefaultAsync(x => x.ItemKey == itemKey, ct);
            if (item != null && !item.IsCharacter) throw Conflict("Existing mission reward item cannot be converted into a shop character.");
            if (item == null) { item = new Item { ItemKey = itemKey, IsCharacter = true }; context.Items.Add(item); }
            item.Name = name; item.IconKey = icon; item.PrefabKey = prefab;
            await context.SaveChangesAsync(ct);
            var product = await context.ShopProducts.SingleOrDefaultAsync(x => x.ItemId == item.Id, ct);
            if (product == null) { product = new ShopProduct { ItemId = item.Id }; context.ShopProducts.Add(product); }
            product.Name = name; product.CoinPrice = request.CoinPrice; product.IsActive = request.IsActive;
            await context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return new() { ProductId = product.Id, CoinPrice = product.CoinPrice.Value, IsActive = product.IsActive,
                Item = ItemView(item, 0, null) };
        }
        catch (DbUpdateException) { context.ChangeTracker.Clear(); throw Unavailable(); }
        catch (System.Data.Common.DbException) { context.ChangeTracker.Clear(); throw Unavailable(); }
    }

    public async Task<ShopProductResponse> SeedExampleAsync(long adminUserId, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        // Repeat-safe and never overwrites a price/visibility edited by an administrator.
        var existing = await (from product in context.ShopProducts.AsNoTracking()
            join item in context.Items.AsNoTracking() on product.ItemId equals (long?)item.Id
            where item.ItemKey == "king-of-knowledge" && item.IsCharacter
            select new { Product = product, Item = item }).SingleOrDefaultAsync(ct);
        if (existing != null) return new() { ProductId = existing.Product.Id,
            CoinPrice = existing.Product.CoinPrice ?? 0, IsActive = existing.Product.IsActive,
            Item = ItemView(existing.Item, 0, null) };
        return await UpsertProductAsync(adminUserId, "king-of-knowledge", new ShopProductRequest
        {
            Name = "King of Knowledge", CoinPrice = 500,
            IconKey = "shop/king-of-knowledge", PrefabKey = "characters/king-of-knowledge"
        }, ct);
    }
}
