namespace Domain.Services
{
    public interface IFileDownloadService
    {
        Task<string> DownloadFile(string url);
    }
}