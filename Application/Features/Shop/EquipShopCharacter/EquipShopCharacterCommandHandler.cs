using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.EquipShopCharacter;

public class EquipShopCharacterCommandHandler(IShopService service) : IRequestHandler<EquipShopCharacterCommand, ShopInventoryResponse>
{
    public Task<ShopInventoryResponse> Handle(EquipShopCharacterCommand request, CancellationToken cancellationToken)
        => service.EquipAsync(request.UserId, request.ItemId, cancellationToken);
}
