using System.Net;

using Domain.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Shared.Enums;

using Shared.Exceptions;
namespace Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IGoogleCloudStorageService _googleCloudStorageService;
        public FileUploadService(IWebHostEnvironment environment, IGoogleCloudStorageService googleCloudStorageService)
        {
            _environment = environment;
            _googleCloudStorageService = googleCloudStorageService;
        }

        public async Task DeleteFile(string path)
        {
            await _googleCloudStorageService.DeleteFileAsync(path);
        }

        public async Task<string> UploadImage(IFormFile file, string path)
        {
            return await UploadFileInternal(file, FileType.Image, path);
        }

        public async Task<string> UploadJson(IFormFile file, string path)
        {
            return await UploadFileInternal(file, FileType.Json, path);
        }

        private async Task<string> UploadFileInternal(IFormFile file, FileType fileType, string path)
        {
            var allowedExtensions = GetAllowedExtensions(fileType);
            ValidateFile(file, allowedExtensions);

            var objectName = GenerateObjectName(file, fileType, path);

            using (var fileStream = file.OpenReadStream())
            {
                await _googleCloudStorageService.UploadFileAsync(fileStream, objectName);
            }

            return objectName;
        }

        private string[] GetAllowedExtensions(FileType fileType)
        {
            return fileType switch
            {
                FileType.Image => new[] { ".jpg", ".jpeg", ".png", ".gif" },
                FileType.Json => new[] { ".json" },
                _ => throw new GenericException(
                    message: "Invalid file type",
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure)
            };
        }

        private void ValidateFile(IFormFile file, string[] allowedExtensions)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (file.Length == 0 || file == null || !allowedExtensions.Contains(extension))
            {
                throw new GenericException(
                    message: "Invalid file type",
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }
        }

        private string GenerateObjectName(IFormFile file, FileType fileType, string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileExtension = Path.GetExtension(file.FileName);
            return $"{path}/{fileName}_{Guid.NewGuid()}{fileExtension}";
        }



    }
}
