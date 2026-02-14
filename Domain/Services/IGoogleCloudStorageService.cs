namespace Domain.Services
{
    public interface IGoogleCloudStorageService
    {
        Task UploadFileAsync(Stream fileStream, string objectName);
        Task DeleteFileAsync(string objectName);
    }
}
