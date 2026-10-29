using MediatR;
using Shared.Responses.Shop;
using Shared.Requests.Shop;
namespace Application.Features.Shop.GetShopHistory;

public class GetShopHistoryQuery : IRequest<List<ShopPurchaseResponse>>
{
    public long UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
