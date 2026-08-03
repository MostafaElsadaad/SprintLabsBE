using System.Net;

using Application.Features.CommunityInvitations.CompleteTeacherInvitation;
using Application.Features.CommunityInvitations.ValidateTeacherInvitation;

using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/community-invitations")]
[ApiController]
[ApiVersion("1.0")]
public class CommunityInvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunityInvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet("teacher/validate")]
    [ProducesResponseType(typeof(BaseResponse<TeacherInvitationValidationResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateTeacherInvitation([FromQuery] string token)
    {
        var result = await _mediator.Send(new ValidateTeacherInvitationQuery { Token = token });
        return Ok(new BaseResponse<TeacherInvitationValidationResult>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }

    [AllowAnonymous]
    [HttpPost("teacher/complete")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteTeacherInvitation([FromBody] CompleteTeacherInvitationRequest request)
    {
        await _mediator.Send(new CompleteTeacherInvitationCommand
        {
            Token = request.Token,
            Name = request.Name,
            Password = request.Password
        });
        return Ok(new BaseResponse<object>(null!, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}