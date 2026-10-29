using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.GetShopInventory;

public class GetShopInventoryQueryHandler(IShopService service) : IRequestHandler<GetShopInventoryQuery, ShopInventoryResponse>
{
    public Task<ShopInventoryResponse> Handle(GetShopInventoryQuery request, CancellationToken cancellationToken)
        => service.InventoryAsync(request.UserId, cancellationToken);
}
