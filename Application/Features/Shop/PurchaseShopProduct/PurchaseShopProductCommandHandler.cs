using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.PurchaseShopProduct;

public class PurchaseShopProductCommandHandler(IShopService service) : IRequestHandler<PurchaseShopProductCommand, ShopPurchaseResponse>
{
    public Task<ShopPurchaseResponse> Handle(PurchaseShopProductCommand request, CancellationToken cancellationToken)
        => service.PurchaseAsync(request.UserId, request.ProductId, request.RequestId, cancellationToken);
}
