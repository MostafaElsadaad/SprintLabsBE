using System.Net;
using API.Authentication;
using Application.Features.Missions.ProcessMissionEvents;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests;
using Shared.Responses;
namespace API.Controllers;
[ApiController, ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game-server/missions/events")]
[RequestSizeLimit(512 * 1024)]
public class GameServerMissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public GameServerMissionsController(IMediator mediator) => _mediator = mediator;
    [HttpPost, Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
    public Task<IActionResult> Single([FromBody] MissionEventRequest request, CancellationToken ct) => Bulk(new() { request }, ct);
    [HttpPost("bulk"), Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> Bulk([FromBody] List<MissionEventRequest> events, CancellationToken ct)
    {
        var data = await _mediator.Send(new ProcessMissionEventsCommand { Events = events }, ct);
        return Ok(new BaseResponse<List<MissionEventResponse>>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
