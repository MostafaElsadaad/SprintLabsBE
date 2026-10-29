using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.SeedShopExample;

public class SeedShopExampleCommandHandler(IShopService service) : IRequestHandler<SeedShopExampleCommand, ShopProductResponse>
{
    public Task<ShopProductResponse> Handle(SeedShopExampleCommand request, CancellationToken cancellationToken)
        => service.SeedExampleAsync(request.UserId, cancellationToken);
}
