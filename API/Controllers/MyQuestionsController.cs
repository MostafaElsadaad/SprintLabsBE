using System.Net;
using Application.Features.TeacherQuestions.DeleteTeacherQuestion;
using Application.Features.TeacherQuestions.ListTeacherQuestions;
using Application.Features.TeacherQuestions.SaveTeacherQuestion;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests.LiveGames;
using Shared.Responses;

namespace API.Controllers;

/// <summary>Private "My Questions" library used when building live games.</summary>
[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/admin/my-questions")]
[Tags("Super Admin Control")]
[RequestSizeLimit(256 * 1024)]
public class MyQuestionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public MyQuestionsController(IMediator mediator) => _mediator = mediator;
    private long? UserId => long.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TeacherQuestionFilter filter, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new ListTeacherQuestionsQuery { AuthenticatedUserId = UserId.Value, Filter = filter }, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TeacherQuestionRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new SaveTeacherQuestionCommand { AuthenticatedUserId = UserId.Value, Request = request }, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TeacherQuestionRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new SaveTeacherQuestionCommand { AuthenticatedUserId = UserId.Value, TeacherQuestionId = id, Request = request }, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new DeleteTeacherQuestionCommand { AuthenticatedUserId = UserId.Value, TeacherQuestionId = id }, ct));
    }

    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
