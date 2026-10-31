using MediatR;
using Shared.Responses.Shop;
namespace Application.Features.Shop.OpenShopBox;

public class OpenShopBoxCommand : IRequest<ShopBoxOpenResponse>
{
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public Guid RequestId { get; set; }
}
