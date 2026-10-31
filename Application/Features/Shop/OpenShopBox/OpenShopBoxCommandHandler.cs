using Domain.Services;
using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.OpenShopBox;

public class OpenShopBoxCommandHandler(IShopService service) : IRequestHandler<OpenShopBoxCommand, ShopBoxOpenResponse>
{
    public Task<ShopBoxOpenResponse> Handle(OpenShopBoxCommand request, CancellationToken cancellationToken)
        => service.OpenBoxAsync(request.UserId, request.ProductId, request.RequestId, cancellationToken);
}
