using Domain.Services;
using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.SeedMagicalBoxes;

public class SeedMagicalBoxesCommandHandler(IShopService service) : IRequestHandler<SeedMagicalBoxesCommand, List<ShopBoxResponse>>
{
    public Task<List<ShopBoxResponse>> Handle(SeedMagicalBoxesCommand request, CancellationToken cancellationToken)
        => service.SeedMagicalBoxesAsync(request.UserId, cancellationToken);
}
