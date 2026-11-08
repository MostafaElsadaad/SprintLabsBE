using System.Net;
using Application.Features.LiveGames.AddLiveGameBot;
using Application.Features.LiveGames.CreateLiveGame;
using Application.Features.LiveGames.EndLiveGame;
using Application.Features.LiveGames.GetLiveGame;
using Application.Features.LiveGames.ListLiveGames;
using Application.Features.LiveGames.StartLiveGame;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.LiveGames;
using Shared.Responses;
using Shared.Responses.LiveGames;

namespace API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/admin/live-games")]
[Tags("Super Admin Control")]
[RequestSizeLimit(16 * 1024)]
public class LiveGamesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LiveGamesController(IMediator mediator) => _mediator = mediator;
    private long? UserId => long.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLiveGameRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new CreateLiveGameCommand { AuthenticatedUserId = UserId.Value, Request = request }, ct));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new ListLiveGamesQuery { AuthenticatedUserId = UserId.Value }, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new GetLiveGameQuery { AuthenticatedUserId = UserId.Value, LiveGameId = id }, ct));
    }

    [HttpPost("{id:long}/start")]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new StartLiveGameCommand { AuthenticatedUserId = UserId.Value, LiveGameId = id }, ct));
    }

    [HttpPost("{id:long}/bots")]
    public async Task<IActionResult> AddBot(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new AddLiveGameBotCommand { AuthenticatedUserId = UserId.Value, LiveGameId = id }, ct));
    }

    [HttpPost("{id:long}/end")]
    public async Task<IActionResult> End(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new EndLiveGameCommand { AuthenticatedUserId = UserId.Value, LiveGameId = id }, ct));
    }

    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
