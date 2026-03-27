using Microsoft.AspNetCore.Mvc;

using Shared.Enums;
using Shared.Responses;
using System.Net;

using Domain.Services;
using MediatR;
using Application.Features.Accounts.GoogleAuthenticate;
using Asp.Versioning;

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

    }
}
