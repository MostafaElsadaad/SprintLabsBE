using System.Net;
using Application.Features.Missions.GetMyMissions;
using Application.Features.Missions.ClaimMissions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Responses;
namespace API.Controllers;
[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/Missions")]
public class MissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public MissionsController(IMediator mediator) => _mediator = mediator;
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var id) || id <= 0) return Unauthorized();
        var data = await _mediator.Send(new GetMyMissionsQuery { UserId = id }, ct);
        return Ok(new BaseResponse<List<PlayerMissionResponse>>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
    [HttpPost("{playerMissionId:long}/claim")]
    public Task<IActionResult> Claim(long playerMissionId, CancellationToken ct) => ClaimCore(playerMissionId, ct);
    [HttpPost("claim-all")]
    public Task<IActionResult> ClaimAll(CancellationToken ct) => ClaimCore(null, ct);
    private async Task<IActionResult> ClaimCore(long? missionId, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var id) || id <= 0) return Unauthorized();
        var data = await _mediator.Send(new ClaimMissionsCommand { UserId = id, MissionId = missionId }, ct);
        return Ok(new BaseResponse<List<MissionClaimResponse>>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
