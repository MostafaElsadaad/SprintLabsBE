using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Shared.Enums;
using Shared.Responses;
using System.Net;

using Domain.Services;
using MediatR;
using Application.Features.Accounts.GoogleAuthenticate;
using Asp.Versioning;
using Application.Features.Questions;
using Shared.Requests.QuestionData;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class QuestionController : ControllerBase
    {
        private readonly IMediator _mediator;
        public QuestionController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetQuestions([FromQuery] QuestionBankFilter filter, int? assignment, CancellationToken ct)
        {
            GetQuestionsQuery getQuestionsQuery = new GetQuestionsQuery
            {
                Grade = filter.Grade,
                Assignment = assignment,
                Filter = filter
            };

            var questions = await _mediator.Send(getQuestionsQuery, ct);
            return Ok(new BaseResponse<GetQuestionsDto>(
                data: questions,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpsertQuestions([FromBody] UpsertQuestionsCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(new BaseResponse<GetQuestionsDto>(
                data: result,
                statusCode: HttpStatusCode.OK,
                errorCode: ErrorCode.Success,
                message: ErrorMessage.Success));
        }

    }
}
