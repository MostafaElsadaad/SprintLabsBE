using System.Net;

using Application.Features.Communities.Common;
using Application.Features.Communities.GetCommunityProfile;
using Application.Features.Communities.UpdateCommunityProfile;

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
public class CommunitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{communityId:long}")]
    public async Task<IActionResult> GetCommunity(long communityId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetCommunityProfileQuery
        {
            UserId = userId.Value,
            CommunityId = communityId
        });

        return Ok(new BaseResponse<CommunityProfileResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    [HttpPatch("{communityId:long}")]
    public async Task<IActionResult> UpdateCommunity(
        long communityId,
        [FromBody] UpdateCommunityProfileRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new UpdateCommunityProfileCommand
        {
            UserId = userId.Value,
            CommunityId = communityId,
            Name = request.Name,
            Slug = request.Slug
        });

        return Ok(new BaseResponse<CommunityProfileResponse>(
            data: result,
            statusCode: HttpStatusCode.OK,
            errorCode: ErrorCode.Success,
            message: ErrorMessage.Success));
    }

    private long? GetUserId()
    {
        var claimValue = HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "userId")?.Value;

        return long.TryParse(claimValue, out var userId)
            ? userId
            : null;
    }
}
