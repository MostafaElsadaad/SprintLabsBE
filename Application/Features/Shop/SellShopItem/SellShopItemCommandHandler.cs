using Domain.Services;
using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.SellShopItem;

public class SellShopItemCommandHandler(IShopService service) : IRequestHandler<SellShopItemCommand, ShopItemSaleResponse>
{
    public Task<ShopItemSaleResponse> Handle(SellShopItemCommand request, CancellationToken cancellationToken)
        => service.SellItemAsync(request.UserId, request.ItemId, request.RequestId, cancellationToken);
}
