using System.Net;

using Application.Features.Users.GetCurrentPlayerProfile;
using Application.Features.Users.GetCurrentUser;
using Application.Features.Users.GetCurrentUserCommunities;

using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new GetCurrentUserQuery
            {
                UserId = userId.Value
            });

            return Ok(new BaseResponse<CurrentUserResponse>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpGet("me/player-profile")]
        public async Task<IActionResult> GetMyPlayerProfile()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new GetCurrentPlayerProfileQuery
            {
                UserId = userId.Value
            });

            return Ok(new BaseResponse<PlayerProfileResponse>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpGet("me/communities")]
        public async Task<IActionResult> GetMyCommunities()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new GetCurrentUserCommunitiesQuery
            {
                UserId = userId.Value
            });

            return Ok(new BaseResponse<List<UserCommunityResponse>>(
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
}
