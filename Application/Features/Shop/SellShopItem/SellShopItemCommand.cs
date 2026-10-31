using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.SellShopItem;

public class SellShopItemCommand : IRequest<ShopItemSaleResponse>
{
    public long UserId { get; set; }
    public long ItemId { get; set; }
    public Guid RequestId { get; set; }
}
