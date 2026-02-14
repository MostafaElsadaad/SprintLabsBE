using Shared.Enums;
using System.Net;

namespace Shared.Exceptions
{
    public class GenericException : HttpRequestException
    {
        public ErrorCode ErrorCode { get; set; }
        public HttpStatusCode? StatusCode { get; set; }

        public GenericException(ErrorCode errorCode, string message = "", HttpStatusCode? statusCode = HttpStatusCode.InternalServerError) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}
