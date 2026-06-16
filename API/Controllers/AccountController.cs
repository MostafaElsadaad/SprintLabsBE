using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

using Application.Features.Accounts.GetPlayer;
using Application.Features.Accounts.GoogleAuthenticate;
using Application.Features.Accounts.UpdateProfile;

using Asp.Versioning;

using Domain.Services;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;
using Application.Features.Accounts.GetPlayer;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(string googleAccessToken)
        {
            GoogleAuthenticationCommand command = new GoogleAuthenticationCommand
            {
                IdToken = googleAccessToken
            };
            var compassAccessToken = await _mediator.Send(command);

            return Ok(new BaseResponse<LoginResponse>(
                 data: compassAccessToken,
                 statusCode: HttpStatusCode.OK,
                 errorCode: ErrorCode.Success,
                 message: ErrorMessage.Success));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            
            var googleId = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(googleId))
            {
                return Unauthorized();
            }

            command.GoogleId = googleId;
            

            var result = await _mediator.Send(command);
            return Ok(result);
        }



        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetPlayer()
        {
            var googleId = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(googleId))
            {
                return Unauthorized();
            }

            var query = new GetPlayerQuery
            {
                GoogleId = googleId
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
