using Application.Features.PlayerProfiles.Common;
using Application.Features.PlayerProfiles.GetPlayerSetup;
using Application.Features.PlayerProfiles.GetSetupOptions;
using Application.Features.PlayerProfiles.SavePlayerSetup;
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
        [HttpPost("me/appearance")]
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
                SchoolName = request.SchoolName,
                AvatarAssetPath = request.AvatarAssetPath,
                ProfilePictureAssetPath = request.ProfilePictureAssetPath
            });

            return Ok(new BaseResponse<PlayerProfileResponse>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpGet("me/setup")]
        public async Task<IActionResult> GetMySetup(CancellationToken cancellationToken)
        {
            var id = GetUserId();
            if (id == null) return Unauthorized();
            var result = await _mediator.Send(new GetPlayerSetupQuery { UserId = id.Value }, cancellationToken);
            return Ok(new BaseResponse<PlayerSetupResponse>(data: result, statusCode: HttpStatusCode.OK, errorCode: ErrorCode.Success, message: ErrorMessage.Success));
        }

        [HttpGet("setup-options")]
        public async Task<IActionResult> GetSetupOptions(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSetupOptionsQuery(), cancellationToken);
            return Ok(new BaseResponse<GetSetupOptionsResponse>(data: result, statusCode: HttpStatusCode.OK, errorCode: ErrorCode.Success, message: ErrorMessage.Success));
        }

        [HttpPost("me/setup")]
        public async Task<IActionResult> SaveMySetup([FromBody] SavePlayerSetupRequest request, CancellationToken cancellationToken)
        {
            var id = GetUserId();
            if (id == null) return Unauthorized();
            var result = await _mediator.Send(new SavePlayerSetupCommand
            {
                UserId = id.Value, Nickname = request.Nickname, Grade = request.Grade,
                Curriculum = request.Curriculum, Language = request.Language
            }, cancellationToken);
            return Ok(new BaseResponse<PlayerSetupResponse>(data: result, statusCode: HttpStatusCode.OK, errorCode: ErrorCode.Success, message: ErrorMessage.Success));
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
