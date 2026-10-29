using Shared.Requests.Shop;
using Shared.Responses.Shop;
namespace Domain.Services;

public interface IShopService
{
    Task<ShopCatalogueResponse> CatalogueAsync(long userId, CancellationToken ct);
    Task<ShopInventoryResponse> InventoryAsync(long userId, CancellationToken ct);
    Task<ShopPurchaseResponse> PurchaseAsync(long userId, long productId, Guid requestId, CancellationToken ct);
    Task<ShopInventoryResponse> EquipAsync(long userId, long? itemId, CancellationToken ct);
    Task<List<ShopPurchaseResponse>> HistoryAsync(long userId, int page, int pageSize, CancellationToken ct);
    Task<ShopProductResponse> UpsertProductAsync(long adminUserId, string itemKey, ShopProductRequest request, CancellationToken ct);
    Task<ShopProductResponse> SeedExampleAsync(long adminUserId, CancellationToken ct);
}
