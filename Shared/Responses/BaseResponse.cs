using Shared.Enums;
using System.Net;

namespace Shared.Responses
{
    public class BaseResponse
    {
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public ErrorCode ErrorCode { get; set; }

        public BaseResponse(string message, HttpStatusCode statusCode, ErrorCode errorCode)
        {
            Message = message;
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }

    public class BaseResponse<TData>
    {
        public TData Data { get; set; }
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public ErrorCode ErrorCode { get; set; }

        public BaseResponse(TData data, string message, HttpStatusCode statusCode, ErrorCode errorCode)
        {
            Data = data;
            Message = message;
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}
