using System.Net;

using Application.Features.PlayerProfiles.UpdateCurrentPlayerProfile;

using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/v{version:apiVersion}/player-profiles")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class PlayerProfilesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlayerProfilesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMyPlayerProfile([FromBody] UpdateCurrentPlayerProfileRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new UpdateCurrentPlayerProfileCommand
            {
                UserId = userId.Value,
                Age = request.Age,
                Grade = request.Grade,
                SchoolName = request.SchoolName
            });

            return Ok(new BaseResponse<PlayerProfileResponse>(
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
