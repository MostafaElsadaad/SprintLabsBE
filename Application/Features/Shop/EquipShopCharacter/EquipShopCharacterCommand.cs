using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.EquipShopCharacter;

public class EquipShopCharacterCommand : IRequest<ShopInventoryResponse>
{
    public long UserId { get; set; }
    public long? ItemId { get; set; }
}
