using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.GetShopInventory;

public class GetShopInventoryQuery : IRequest<ShopInventoryResponse>
{
    public long UserId { get; set; }
}
