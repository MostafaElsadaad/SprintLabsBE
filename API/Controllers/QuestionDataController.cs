using System.Net;
using API.Authentication;
using Application.Features.Questions.GetQuestionBank;
using Application.Features.Questions.GetDisplayQuestions;
using Application.Features.Questions.ImportLegacyQuestions;
using Application.Features.Questions.GetQuestionHistory;
using Application.Features.Questions.PublishQuestion;
using Application.Features.Questions.RecordQuestionHistory;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace API.Controllers;

[ApiController, ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/Question")]
public class QuestionDataController(IMediator mediator) : ControllerBase
{
    [HttpPost("bank/import-legacy/{packId:long}"), Authorize]
    public async Task<IActionResult> ImportLegacy(long packId, CancellationToken ct)
    {
        if (!UserId(out var id)) return Unauthorized();
        return Success(await mediator.Send(new ImportLegacyQuestionsCommand { UserId = id, PackId = packId }, ct));
    }

    [HttpGet("bank"), Authorize]
    public async Task<IActionResult> Display([FromQuery] QuestionBankFilter filter, CancellationToken ct)
    {
        if (!UserId(out var id)) return Unauthorized();
        return Success(await mediator.Send(new GetDisplayQuestionsQuery { UserId = id, Filter = filter }, ct));
    }

    [HttpPut("bank"), Authorize, RequestSizeLimit(1024 * 1024)]
    public async Task<IActionResult> Publish([FromBody] QuestionDataRequest request, CancellationToken ct)
    {
        if (!UserId(out var id)) return Unauthorized();
        var data = await mediator.Send(new PublishQuestionCommand { UserId = id, Request = request }, ct);
        return Success(data);
    }

    [HttpGet("bank/admin"), Authorize]
    public async Task<IActionResult> AdminBank([FromQuery] QuestionBankFilter filter, CancellationToken ct)
    {
        if (!UserId(out var id)) return Unauthorized();
        return Success(await mediator.Send(new GetQuestionBankQuery { UserId = id, Filter = filter }, ct));
    }

    [HttpGet("bank/server"), Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> ServerBank([FromQuery] QuestionBankFilter filter, CancellationToken ct)
        => Success(await mediator.Send(new GetQuestionBankQuery { IsGameServer = true, Filter = filter }, ct));

    [HttpPost("history"), Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName), RequestSizeLimit(128 * 1024)]
    public async Task<IActionResult> Record([FromBody] QuestionHistoryRequest request, CancellationToken ct)
        => Success(await mediator.Send(new RecordQuestionHistoryCommand { Request = request }, ct));

    [HttpPut("history/{historyId:long}"), Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName), RequestSizeLimit(128 * 1024)]
    public async Task<IActionResult> Replay(long historyId, [FromBody] QuestionHistoryRequest request, CancellationToken ct)
        => Success(await mediator.Send(new RecordQuestionHistoryCommand { Request = request, HistoryId = historyId }, ct));

    [HttpGet("history/players/{playerId:long}"), Authorize]
    public async Task<IActionResult> History(long playerId, long? matchId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        if (!UserId(out var id)) return Unauthorized();
        return Success(await mediator.Send(new GetQuestionHistoryQuery { UserId = id, PlayerId = playerId, MatchId = matchId, PageNumber = pageNumber, PageSize = pageSize }, ct));
    }

    [HttpGet("history/server/players/{playerId:long}"), Authorize(AuthenticationSchemes = GameServerAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> Export(long playerId, long? matchId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
        => Success(await mediator.Send(new GetQuestionHistoryQuery { PlayerId = playerId, MatchId = matchId, PageNumber = pageNumber, PageSize = pageSize }, ct));

    private bool UserId(out long id) => long.TryParse(User.FindFirst("userId")?.Value, out id) && id > 0;
    private IActionResult Success<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
