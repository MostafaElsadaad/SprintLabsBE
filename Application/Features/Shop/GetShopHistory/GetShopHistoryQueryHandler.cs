using MediatR;
using Domain.Services;
using Shared.Responses.Shop;
namespace Application.Features.Shop.GetShopHistory;

public class GetShopHistoryQueryHandler(IShopService service) : IRequestHandler<GetShopHistoryQuery, List<ShopPurchaseResponse>>
{
    public Task<List<ShopPurchaseResponse>> Handle(GetShopHistoryQuery request, CancellationToken cancellationToken)
        => service.HistoryAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}
