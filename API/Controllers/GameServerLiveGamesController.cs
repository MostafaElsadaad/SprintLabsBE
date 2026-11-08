using System.Net;
using API.Authentication;
using Application.Features.LiveGames.GetActiveLiveGames;
using Application.Features.LiveGames.GetLiveGameQuestions;
using Application.Features.LiveGames.RecordLiveGameEvents;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.LiveGames;
using Shared.Responses;

namespace API.Controllers;

[ApiController, ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/game-server/live-games")]
[Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
[RequestSizeLimit(256 * 1024)]
public class GameServerLiveGamesController : ControllerBase
{
    private readonly IMediator _mediator;
    public GameServerLiveGamesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("active")]
    public async Task<IActionResult> Active(CancellationToken ct)
        => Envelope(await _mediator.Send(new GetActiveLiveGamesQuery(), ct));

    [HttpPost("{code}/events")]
    public async Task<IActionResult> Events(string code, [FromBody] List<LiveGameEventRequest> events, CancellationToken ct)
        => Envelope(await _mediator.Send(new RecordLiveGameEventsCommand { Code = code, Events = events }, ct));

    [HttpGet("{code}/questions")]
    public async Task<IActionResult> Questions(string code, CancellationToken ct)
        => Envelope(await _mediator.Send(new GetLiveGameQuestionsQuery { Code = code }, ct));

    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
