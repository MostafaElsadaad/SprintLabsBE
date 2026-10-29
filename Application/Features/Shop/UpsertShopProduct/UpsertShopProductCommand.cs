using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.UpsertShopProduct;

public class UpsertShopProductCommand : IRequest<ShopProductResponse>
{
    public long UserId { get; set; }
    public string ItemKey { get; set; } = string.Empty;
    public ShopProductRequest Request { get; set; } = new();
}
