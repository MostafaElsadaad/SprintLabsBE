using Domain.Services;
using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.EquipShopItem;

public class EquipShopItemCommandHandler(IShopService service) : IRequestHandler<EquipShopItemCommand, ShopInventoryResponse>
{
    public Task<ShopInventoryResponse> Handle(EquipShopItemCommand request, CancellationToken cancellationToken)
        => service.EquipItemAsync(request.UserId, request.ItemId, request.Category, cancellationToken);
}
