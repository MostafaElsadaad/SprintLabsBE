using System.Net;
using Application.Features.Progression.GetProgression;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController, ApiVersion("1.0"), Authorize]
public class ProgressionController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProgressionController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public Task<IActionResult> Me(CancellationToken ct) => Get(null, ct);

    [HttpGet("players/{playerProfileId:long}")]
    public Task<IActionResult> Player(long playerProfileId, CancellationToken ct) => Get(playerProfileId, ct);

    private async Task<IActionResult> Get(long? playerId, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetProgressionQuery { UserId = userId, PlayerProfileId = playerId }, ct);
        return Ok(new BaseResponse<PlayerProgressionResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
