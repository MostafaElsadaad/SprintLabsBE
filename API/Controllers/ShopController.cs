using Application.Features.Shop.PurchaseShopBox;
using Application.Features.Shop.OpenShopBox;
using Application.Features.Shop.SellShopItem;
using Application.Features.Shop.EquipShopItem;
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
    [HttpPost("boxes/{productId:long}/purchase")]
    public async Task<IActionResult> PurchaseBox(long productId, [FromBody] ShopPurchaseRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new PurchaseShopBoxCommand {UserId=UserId.Value,ProductId=productId,RequestId=request.RequestId},ct));
    }
    [HttpPost("me/boxes/{productId:long}/open")]
    public async Task<IActionResult> OpenBox(long productId, [FromBody] ShopPurchaseRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new OpenShopBoxCommand {UserId=UserId.Value,ProductId=productId,RequestId=request.RequestId},ct));
    }
    [HttpPost("me/items/{itemId:long}/sell")]
    public async Task<IActionResult> SellItem(long itemId, [FromBody] ShopPurchaseRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new SellShopItemCommand {UserId=UserId.Value,ItemId=itemId,RequestId=request.RequestId},ct));
    }
    [HttpPut("me/equipped-item")]
    public async Task<IActionResult> EquipItem([FromBody] EquipShopItemRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new EquipShopItemCommand {UserId=UserId.Value,ItemId=request.ItemId,Category=request.Category},ct));
    }
    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
