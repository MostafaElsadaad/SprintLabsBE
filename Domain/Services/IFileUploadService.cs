using Microsoft.AspNetCore.Http;

namespace Domain.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadJson(IFormFile file, string path);
        Task<string> UploadImage(IFormFile file, string path);
        Task DeleteFile(string path);
    }
}
