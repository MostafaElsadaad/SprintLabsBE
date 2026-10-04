using System.Net;
using Application.Features.Progression.GetRanking;
using Application.Features.Progression.GetLeaderboard;
using Application.Features.Progression.GetLeaderboardStanding;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController, ApiVersion("1.0"), Authorize]
public class RankingController : ControllerBase
{
    private readonly IMediator _mediator;
    public RankingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public Task<IActionResult> Me(CancellationToken ct) => Get(null, ct);
    [HttpGet("players/{playerProfileId:long}")]
    public Task<IActionResult> Player(long playerProfileId, CancellationToken ct) => Get(playerProfileId, ct);

    [HttpGet("leaderboard")]
    public Task<IActionResult> GlobalLeaderboard([FromQuery] LeaderboardRequest page, CancellationToken ct)
        => Leaderboard(null, page, ct);
    [HttpGet("/api/v{version:apiVersion}/Communities/{communityId:long}/ranking/leaderboard")]
    public Task<IActionResult> CommunityLeaderboard(long communityId, [FromQuery] LeaderboardRequest page, CancellationToken ct)
        => Leaderboard(communityId, page, ct);

    [HttpGet("leaderboard/me")]
    public Task<IActionResult> GlobalStanding([FromQuery] LeaderboardRequest request, CancellationToken ct)
        => Standing(null, request, ct);
    [HttpGet("/api/v{version:apiVersion}/Communities/{communityId:long}/ranking/leaderboard/me")]
    public Task<IActionResult> CommunityStanding(long communityId, [FromQuery] LeaderboardRequest request, CancellationToken ct)
        => Standing(communityId, request, ct);

    private async Task<IActionResult> Get(long? playerId, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetRankingQuery { UserId = userId, PlayerProfileId = playerId }, ct);
        return Ok(new BaseResponse<PlayerRankingResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
    private async Task<IActionResult> Leaderboard(long? communityId, LeaderboardRequest page, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetLeaderboardQuery { UserId = userId, CommunityId = communityId, Page = page }, ct);
        return Ok(new BaseResponse<LeaderboardPageResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
    private async Task<IActionResult> Standing(long? communityId, LeaderboardRequest request, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var userId) || userId <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetLeaderboardStandingQuery { UserId = userId, CommunityId = communityId, Request = request }, ct);
        return Ok(new BaseResponse<LeaderboardStandingResponse>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
