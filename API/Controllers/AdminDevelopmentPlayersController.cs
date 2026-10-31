using Application.Features.Accounts.DevelopmentAuthentication.SeedLeaderboardDemo;
using System.Net;
using Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums;
using Shared.Options;
using Shared.Responses;

namespace API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/admin/development-players")]
[RequestSizeLimit(16 * 1024)]
public sealed class AdminDevelopmentPlayersController(IMediator mediator) : ControllerBase
{
    [HttpPost("leaderboard-demo")]
    public async Task<IActionResult> SeedLeaderboardDemo([FromBody] SeedLeaderboardDemoRequest request, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var adminId) || adminId <= 0) return Unauthorized();
        var result = await mediator.Send(new SeedLeaderboardDemoCommand
        {
            AdminUserId = adminId, CommunityId = request.CommunityId, GradeId = request.GradeId, BaseClassId = request.BaseClassId,
            SuppliedApiKeys = Request.Headers[DevelopmentAuthenticationOptions.HeaderName].Where(v => v != null).Select(v => v!).ToArray()
        }, ct);
        return Ok(new BaseResponse<SeedLeaderboardDemoResponse>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
    [HttpPost("{accountKey}/school")]
    public async Task<IActionResult> AssignSchool(string accountKey, [FromBody] AssignDevelopmentSchoolRequest request, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirst("userId")?.Value, out var adminId) || adminId <= 0) return Unauthorized();
        var result = await mediator.Send(new AssignDevelopmentSchoolCommand
        {
            AdminUserId = adminId, AccountKey = accountKey, CommunityId = request.CommunityId,
            GradeId = request.GradeId, ClassId = request.ClassId,
            SuppliedApiKeys = Request.Headers[DevelopmentAuthenticationOptions.HeaderName].Where(v => v != null).Select(v => v!).ToArray()
        }, ct);
        return Ok(new BaseResponse<AssignDevelopmentSchoolResponse>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
    }
}
