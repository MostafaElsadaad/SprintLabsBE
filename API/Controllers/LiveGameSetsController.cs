using System.Net;
using Application.Features.LiveGameSets.DeleteLiveGameSet;
using Application.Features.LiveGameSets.ListLiveGameSets;
using Application.Features.LiveGameSets.SaveLiveGameSet;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.LiveGames;
using Shared.Responses;

namespace API.Controllers;

/// <summary>Saved question sets ("My Saved Games / Sets") for launching new live games.</summary>
[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/admin/live-game-sets")]
[Tags("Super Admin Control")]
[RequestSizeLimit(1024 * 1024)]
public class LiveGameSetsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LiveGameSetsController(IMediator mediator) => _mediator = mediator;
    private long? UserId => long.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new ListLiveGameSetsQuery { AuthenticatedUserId = UserId.Value }, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        var sets = await _mediator.Send(new ListLiveGameSetsQuery { AuthenticatedUserId = UserId.Value, LiveGameSetId = id }, ct);
        return Envelope(sets[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LiveGameQuestionSetRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new SaveLiveGameSetCommand { AuthenticatedUserId = UserId.Value, Request = request }, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] LiveGameQuestionSetRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new SaveLiveGameSetCommand { AuthenticatedUserId = UserId.Value, LiveGameSetId = id, Request = request }, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new DeleteLiveGameSetCommand { AuthenticatedUserId = UserId.Value, LiveGameSetId = id }, ct));
    }

    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
