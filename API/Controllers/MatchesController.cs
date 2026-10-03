using System.Net;
using API.Authentication;
using Application.Features.Progression.RegisterMatch;
using Application.Features.Progression.CompleteMatch;
using Application.Features.Progression.GetMatch;
using Application.Features.Progression.GetMatchHistory;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController, ApiVersion("1.0")]
public class MatchesController : ControllerBase
{
    private readonly IMediator _mediator;
    public MatchesController(IMediator mediator) => _mediator = mediator;

    [HttpPost, Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Register([FromBody] RegisterMatchRequest request, CancellationToken ct)
    {
        var data = await _mediator.Send(new RegisterMatchCommand { Request = request }, ct);
        return Ok(new BaseResponse<MatchRegistrationResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }

    [HttpPost("{matchId:long}/complete"), Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Complete(long matchId, [FromBody] CompleteMatchRequest request, CancellationToken ct)
    {
        var data = await _mediator.Send(new CompleteMatchCommand { MatchId = matchId, Request = request }, ct);
        return Ok(new BaseResponse<MatchCompletionResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }

    [HttpGet("me/history"), Authorize]
    public Task<IActionResult> History([FromQuery] ProgressionPageRequest page, CancellationToken ct) => History(null, null, page, ct);
    [HttpGet("/api/v{version:apiVersion}/Communities/{communityId:long}/matches"), Authorize]
    public Task<IActionResult> CommunityHistory(long communityId, [FromQuery] ProgressionPageRequest page, CancellationToken ct)
        => History(communityId, null, page, ct);
    [HttpGet("/api/v{version:apiVersion}/Communities/{communityId:long}/players/{playerProfileId:long}/matches"), Authorize]
    public Task<IActionResult> CommunityPlayerHistory(long communityId, long playerProfileId, [FromQuery] ProgressionPageRequest page, CancellationToken ct)
        => History(communityId, playerProfileId, page, ct);

    [HttpGet("{matchId:long}"), Authorize]
    public async Task<IActionResult> Detail(long matchId, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetMatchQuery { UserId = userId, MatchId = matchId }, ct);
        return Ok(new BaseResponse<MatchDetailResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }

    private async Task<IActionResult> History(long? communityId, long? playerId, ProgressionPageRequest page, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetMatchHistoryQuery
        { UserId = userId, CommunityId = communityId, PlayerProfileId = playerId, Page = page }, ct);
        return Ok(new BaseResponse<ProgressionPage<MatchHistoryResponse>>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
