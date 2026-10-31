using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.SeedMagicalBoxes;

public class SeedMagicalBoxesCommand : IRequest<List<ShopBoxResponse>>
{
    public long UserId { get; set; }

}
