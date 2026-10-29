using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.SeedShopExample;

public class SeedShopExampleCommand : IRequest<ShopProductResponse>
{
    public long UserId { get; set; }
}
