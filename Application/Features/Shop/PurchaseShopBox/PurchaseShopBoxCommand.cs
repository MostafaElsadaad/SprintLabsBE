using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.PurchaseShopBox;

public class PurchaseShopBoxCommand : IRequest<ShopBoxPurchaseResponse>
{
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public Guid RequestId { get; set; }
}
