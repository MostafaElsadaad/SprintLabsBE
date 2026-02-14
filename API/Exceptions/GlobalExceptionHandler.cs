using System.ComponentModel.DataAnnotations;
using System.Net;


using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace API.Exceptions
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError($"Something went wrong: {exception}");

            BaseResponse baseResponse = exception switch
            {
                ValidationException ex => new BaseResponse(message: ex.Message, statusCode: HttpStatusCode.BadRequest, errorCode: ErrorCode.ValidationError),
                ApplicationException ex => new BaseResponse(message: ex.Message, statusCode: HttpStatusCode.BadRequest, errorCode: ErrorCode.Failure),
                KeyNotFoundException ex => new BaseResponse(message: ex.Message, statusCode: HttpStatusCode.BadRequest, errorCode: ErrorCode.Failure),

                GenericException customApplicationException => new BaseResponse(
                message: customApplicationException.Message,
                errorCode: customApplicationException.ErrorCode,
                statusCode: customApplicationException.StatusCode ?? HttpStatusCode.BadRequest
                ),
                _ => new BaseResponse(message: exception.Message, statusCode: HttpStatusCode.BadRequest, errorCode: ErrorCode.Failure)
            };

            _logger.LogInformation("Exception ResponseData", baseResponse);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)baseResponse.StatusCode;
            await context.Response.WriteAsJsonAsync(baseResponse);
        }
    }
}
