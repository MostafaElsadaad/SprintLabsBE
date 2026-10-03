using System.Net;
using Application.Features.CommunityDashboard.Common;
using Application.Features.CommunityDashboard.StaffIdentity;
using Application.Features.CommunityDashboard.StaffDashboard;
using Application.Features.CommunityDashboard.SearchDashboard;
using Application.Features.CommunityDashboard.ListDashboardNotifications;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CommunityDashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public CommunityDashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public Task<IActionResult> GetStaffIdentity() => Send(new StaffIdentityQuery { UserId = CallerId() }, DashboardResponseMapper.Identity);

    [HttpGet("dashboard")]
    public Task<IActionResult> GetDashboard([FromQuery] StaffDashboardQuery query)
    {
        query.UserId = CallerId();
        return Send(query, DashboardResponseMapper.Dashboard);
    }

    [HttpGet("search")]
    public Task<IActionResult> Search([FromQuery] SearchDashboardQuery query)
    {
        query.UserId = CallerId();
        return Send(query);
    }

    [HttpGet("notifications")]
    public Task<IActionResult> Notifications() => Send(new ListDashboardNotificationsQuery { UserId = CallerId() });

    private async Task<IActionResult> Send<T>(IRequest<T> request, Func<T, object>? map = null)
    {
        var result = await _mediator.Send(request, HttpContext.RequestAborted);
        return Ok(new BaseResponse<object>(map == null ? result! : map(result), ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }

    private long CallerId()
    {
        var raw = User.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
        if (!long.TryParse(raw, out var id) || id <= 0)
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        return id;
    }
}
