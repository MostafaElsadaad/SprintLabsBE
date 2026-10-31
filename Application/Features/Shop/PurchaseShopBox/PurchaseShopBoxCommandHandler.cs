using Domain.Services;
using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.PurchaseShopBox;

public class PurchaseShopBoxCommandHandler(IShopService service) : IRequestHandler<PurchaseShopBoxCommand, ShopBoxPurchaseResponse>
{
    public Task<ShopBoxPurchaseResponse> Handle(PurchaseShopBoxCommand request, CancellationToken cancellationToken)
        => service.PurchaseBoxAsync(request.UserId, request.ProductId, request.RequestId, cancellationToken);
}
