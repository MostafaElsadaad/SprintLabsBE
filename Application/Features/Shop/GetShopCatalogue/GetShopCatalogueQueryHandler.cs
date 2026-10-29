using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.GetShopCatalogue;

public class GetShopCatalogueQueryHandler(IShopService service) : IRequestHandler<GetShopCatalogueQuery, ShopCatalogueResponse>
{
    public Task<ShopCatalogueResponse> Handle(GetShopCatalogueQuery request, CancellationToken cancellationToken)
        => service.CatalogueAsync(request.UserId, cancellationToken);
}
