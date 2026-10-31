using Shared.Requests.Shop;
using Shared.Responses.Shop;
namespace Domain.Services;

public interface IShopService
{
    Task<ShopBoxPurchaseResponse> PurchaseBoxAsync(long userId, long productId, Guid requestId, CancellationToken ct);
    Task<ShopBoxOpenResponse> OpenBoxAsync(long userId, long productId, Guid requestId, CancellationToken ct);
    Task<ShopItemSaleResponse> SellItemAsync(long userId, long itemId, Guid requestId, CancellationToken ct);
    Task<ShopInventoryResponse> EquipItemAsync(long userId, long? itemId, string? category, CancellationToken ct);
    Task<List<ShopBoxResponse>> SeedMagicalBoxesAsync(long adminUserId, CancellationToken ct);
    Task<ShopCatalogueResponse> CatalogueAsync(long userId, CancellationToken ct);
    Task<ShopInventoryResponse> InventoryAsync(long userId, CancellationToken ct);
    Task<ShopPurchaseResponse> PurchaseAsync(long userId, long productId, Guid requestId, CancellationToken ct);
    Task<ShopInventoryResponse> EquipAsync(long userId, long? itemId, CancellationToken ct);
    Task<List<ShopPurchaseResponse>> HistoryAsync(long userId, int page, int pageSize, CancellationToken ct);
    Task<ShopProductResponse> UpsertProductAsync(long adminUserId, string itemKey, ShopProductRequest request, CancellationToken ct);
    Task<ShopProductResponse> SeedExampleAsync(long adminUserId, CancellationToken ct);
}
