using System.Net;
using Application.Features.Shop.GetShopCatalogue;
using Application.Features.Shop.GetShopInventory;
using Application.Features.Shop.PurchaseShopProduct;
using Application.Features.Shop.EquipShopCharacter;
using Application.Features.Shop.GetShopHistory;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.Shop;
using Shared.Responses;
namespace API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/Shop")]
public class ShopController(IMediator mediator) : ControllerBase
{
    private long? UserId => long.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;

    [HttpGet]
    public async Task<IActionResult> Catalogue(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new GetShopCatalogueQuery { UserId = UserId.Value }, ct));
    }
    [HttpGet("me/inventory")]
    public async Task<IActionResult> Inventory(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new GetShopInventoryQuery { UserId = UserId.Value }, ct));
    }
    [HttpPost("products/{productId:long}/purchase")]
    public async Task<IActionResult> Purchase(long productId, [FromBody] ShopPurchaseRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new PurchaseShopProductCommand
            { UserId = UserId.Value, ProductId = productId, RequestId = request.RequestId }, ct));
    }
    [HttpPut("me/equipped-character")]
    public async Task<IActionResult> Equip([FromBody] EquipShopItemRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new EquipShopCharacterCommand { UserId = UserId.Value, ItemId = request.ItemId }, ct));
    }
    [HttpGet("me/purchases")]
    public async Task<IActionResult> History([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new GetShopHistoryQuery { UserId = UserId.Value, Page = page, PageSize = pageSize }, ct));
    }
    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
