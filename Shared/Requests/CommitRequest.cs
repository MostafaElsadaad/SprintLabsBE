using Microsoft.AspNetCore.Http;

namespace Shared.Requests
{
    public class CommitRequest
    {
        public required string Author { get; set; }
        public required string Message { get; set; }
        public List<string>? FilesToDelete { get; set; }
        public List<IFormFile>? FilesToCommit { get; set; }
    }
}