using System.Net;
using Application.Features.Shop.UpsertShopProduct;
using Application.Features.Shop.SeedShopExample;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.Shop;
using Shared.Responses;
namespace API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/admin/shop")]
[RequestSizeLimit(16 * 1024)]
public class AdminShopController(IMediator mediator) : ControllerBase
{
    private long? UserId => long.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;

    [HttpPut("products/{itemKey}")]
    public async Task<IActionResult> Upsert(string itemKey, [FromBody] ShopProductRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new UpsertShopProductCommand
            { UserId = UserId.Value, ItemKey = itemKey, Request = request }, ct));
    }
    [HttpPost("seed/example")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await mediator.Send(new SeedShopExampleCommand { UserId = UserId.Value }, ct));
    }
    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
