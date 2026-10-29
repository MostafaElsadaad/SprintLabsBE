using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.GetShopCatalogue;

public class GetShopCatalogueQuery : IRequest<ShopCatalogueResponse>
{
    public long UserId { get; set; }
}
