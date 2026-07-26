using System.Net;
using System.Security.Claims;

using Application.Features.CommunityInvitations.AcceptInvitation;

using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CommunityInvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CommunityInvitationsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest request)
    {
        var claim = User.FindFirstValue("userId");
        if (!long.TryParse(claim, out var userId)) return Unauthorized();
        var result = await _mediator.Send(new AcceptInvitationCommand { UserId = userId, Token = request.Token });
        return Ok(new BaseResponse<TeacherInvitationAcceptanceResponse>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
