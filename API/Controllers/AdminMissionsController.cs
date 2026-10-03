using Application.Features.Missions.CreateMissionTemplate;
using Application.Features.Missions.CreateMissionActivation;
using Application.Features.Missions.PreviewMissionTemplates;
using Application.Features.Missions.ResetMissions;
using Application.Features.Missions.SeedMissions;
using System.Net;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Requests;
using Shared.Responses;
namespace API.Controllers;
[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/admin/missions")]
[RequestSizeLimit(128 * 1024)]
public class AdminMissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminMissionsController(IMediator mediator) => _mediator = mediator;
    private long? UserId => long.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;
    [HttpPost("templates")]
    public async Task<IActionResult> Template([FromBody] MissionTemplateRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new CreateMissionTemplateCommand { UserId = UserId.Value, Request = request }, ct));
    }
    [HttpGet("templates")]
    public async Task<IActionResult> Templates(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new PreviewMissionTemplatesQuery { UserId = UserId.Value }, ct));
    }
    [HttpPost("activations")]
    public async Task<IActionResult> Activation([FromBody] MissionActivationRequest request, CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new CreateMissionActivationCommand { UserId = UserId.Value, Request = request }, ct));
    }
    [HttpPost("reset/run")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new ResetMissionsCommand { UserId = UserId.Value }, ct));
    }
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (!UserId.HasValue) return Unauthorized();
        return Envelope(await _mediator.Send(new SeedMissionsCommand { UserId = UserId.Value }, ct));
    }
    private IActionResult Envelope<T>(T data) => Ok(new BaseResponse<T>(data, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
}
