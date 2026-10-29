using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.PurchaseShopProduct;

public class PurchaseShopProductCommand : IRequest<ShopPurchaseResponse>
{
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public Guid RequestId { get; set; }
}
