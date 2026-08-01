using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

using Application.Features.Accounts.GoogleAuthenticate;
using Application.Features.Accounts.FirebaseAuthenticate;
using Application.Features.Accounts.UpdateProfile;
using Application.Features.Accounts.TeacherAuthentication.RegisterTeacher;
using Application.Features.Accounts.TeacherAuthentication.ConfirmEmail;
using Application.Features.Accounts.TeacherAuthentication.ResendConfirmation;
using Application.Features.Accounts.TeacherAuthentication.TeacherLogin;
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
        [HttpPost("teachers/register")]
        public async Task<IActionResult> RegisterTeacher([FromBody] RegisterTeacherRequest request)
        {
            var result = await _mediator.Send(new RegisterTeacherCommand { Name = request.Name, Email = request.Email, Password = request.Password });
            return Accepted(new BaseResponse<RegisterTeacherResponse>(result, result.Message, HttpStatusCode.Accepted, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            await _mediator.Send(new ConfirmEmailCommand { UserId = request.UserId, Token = request.Token });
            return Ok(new BaseResponse<object>(null!, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            var result = await _mediator.Send(new ResendConfirmationCommand { Email = request.Email });
            return Accepted(new BaseResponse<ResendConfirmationResponse>(result, result.Message, HttpStatusCode.Accepted, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("teachers/login")]
        public async Task<IActionResult> TeacherLogin([FromBody] TeacherLoginRequest request)
        {
            var result = await _mediator.Send(new TeacherLoginCommand { Email = request.Email, Password = request.Password, CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() });
            return Ok(new BaseResponse<Shared.Responses.TeacherLoginResponse>(result, ErrorMessage.Success, HttpStatusCode.OK, ErrorCode.Success));
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
            await _mediator.Send(new ForgotPasswordCommand { Email = request.Email });
            return Ok(new BaseResponse<object>(new { message = "If an account exists, a password reset email has been sent." }, "If an account exists, a password reset email has been sent.", HttpStatusCode.OK, ErrorCode.Success));
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _mediator.Send(new ResetPasswordCommand { Email = request.Email, Token = request.Token, NewPassword = request.NewPassword });
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
