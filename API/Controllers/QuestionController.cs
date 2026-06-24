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


        [HttpGet]
        public async Task<IActionResult> GetQuestions(int grade, int? assignment)
        {
            GetQuestionsQuery getQuestionsQuery = new GetQuestionsQuery
            {
                Grade = grade,
                Assignment = assignment
            };

            var questions = await _mediator.Send(getQuestionsQuery);
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
