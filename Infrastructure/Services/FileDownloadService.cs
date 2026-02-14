using Domain.Services;

namespace Infrastructure.Services
{
    public class FileDownloadService : IFileDownloadService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FileDownloadService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        
        public async Task<string> DownloadFile(string url)
        {
            var client = _httpClientFactory.CreateClient();
            
            return await client.GetStringAsync(url);
        }
    }
}