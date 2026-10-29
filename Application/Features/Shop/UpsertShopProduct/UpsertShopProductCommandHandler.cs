using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.UpsertShopProduct;

public class UpsertShopProductCommandHandler(IShopService service) : IRequestHandler<UpsertShopProductCommand, ShopProductResponse>
{
    public Task<ShopProductResponse> Handle(UpsertShopProductCommand request, CancellationToken cancellationToken)
        => service.UpsertProductAsync(request.UserId, request.ItemKey, request.Request, cancellationToken);
}
