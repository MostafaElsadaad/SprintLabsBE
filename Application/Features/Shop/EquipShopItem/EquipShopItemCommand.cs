using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.EquipShopItem;

public class EquipShopItemCommand : IRequest<ShopInventoryResponse>
{
    public long UserId { get; set; }
    public long? ItemId { get; set; }
    public string? Category { get; set; }
}
