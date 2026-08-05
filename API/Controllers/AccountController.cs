using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

using Application.Features.Accounts.GoogleAuthenticate;
using Application.Features.Accounts.FirebaseAuthenticate;
using Application.Features.Accounts.UpdateProfile;
using Application.Features.Accounts.CommunityAuthentication.CommunityLogin;
using Application.Features.Accounts.CommunityAuthentication.RegisterCommunity;
using Application.Features.Accounts.TeacherAuthentication.RefreshToken;
using Application.Features.Accounts.TeacherAuthentication.Logout;
using Application.Features.Accounts.TeacherAuthentication.ForgotPassword;
using Application.Features.Accounts.TeacherAuthentication.ResetPassword;

using Asp.Versioning;

using Domain.Services;

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

        [AllowAnonymous]
        [HttpPost("firebase-login")]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseAuthenticationRequest request)
        {
            var loginResponse = await _mediator.Send(new FirebaseAuthenticationCommand { IdToken = request.IdToken });
            return Ok(new BaseResponse<LoginResponse>(
                data: loginResponse,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [AllowAnonymous]
        [HttpPost("community-login")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(BaseResponse<CommunityLoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CommunityLogin([FromBody] CommunityLoginRequest request)
        {
            var result = await _mediator.Send(new CommunityLoginCommand { Identifier = request.Identifier, Password = request.Password, CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() });
            return Ok(new BaseResponse<CommunityLoginResponse>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("community-register")]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterCommunity([FromBody] RegisterCommunityRequest request)
        {
            await _mediator.Send(new RegisterCommunityCommand
            {
                Token = request.Token,
                Name = request.Name,
                Password = request.Password
            });
            return Ok(new BaseResponse<object>(null!, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _mediator.Send(new RefreshTokenCommand { RefreshToken = request.RefreshToken, RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() });
            return Ok(new BaseResponse<Shared.Responses.TeacherTokenResponse>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _mediator.Send(new LogoutCommand { RefreshToken = request.RefreshToken, RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() });
            return Ok(new BaseResponse<object>(null!, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _mediator.Send(new ForgotPasswordCommand { Identifier = request.Identifier });
            const string message = "If a matching account exists, password reset instructions have been sent.";
            return Accepted(new BaseResponse<object>(new { message }, message, HttpStatusCode.Accepted, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _mediator.Send(new ResetPasswordCommand { UserId = request.UserId, Token = request.Token, NewPassword = request.NewPassword });
            return Ok(new BaseResponse<object>(null!, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {

            var userIdValue = HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == "userId")?.Value;

            if (!long.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            command.UserId = userId;


            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
