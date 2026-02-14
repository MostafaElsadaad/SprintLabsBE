using Domain.Services;

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

using Microsoft.Extensions.Options;

using Shared.Options;

namespace Infrastructure.Services
{
    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly string _serviceAccountKeyPath;
        private readonly string _bucketName;
        private readonly StorageClient _storageClient;

        public GoogleCloudStorageService(IOptions<GoogleCloudStorageOptions> options)
        {
            var config = options.Value;
            _serviceAccountKeyPath = config.ServiceAccountKeyPath;
            _bucketName = config.BucketName;
            _storageClient = InitializeStorageClient().Result;
        }
        private async Task<StorageClient> InitializeStorageClient()
        {
            // Stream that reads the service account key file
            await using var serviceAccountJsonKeyFileStream = File.OpenRead(_serviceAccountKeyPath);

            // Service account credentials
            var serviceAccountCredential = ServiceAccountCredential.FromServiceAccountData(serviceAccountJsonKeyFileStream);
            var credential = GoogleCredential.FromServiceAccountCredential(serviceAccountCredential);

            // Initialize and return the Storage Client
            var storageClient = await StorageClient.CreateAsync(credential);
            return storageClient;


        }

        public async Task UploadFileAsync(Stream fileStream, string objectName)
        {
            await _storageClient.UploadObjectAsync(_bucketName, objectName, null, fileStream);
        }

        public async Task DeleteFileAsync(string objectName)
        {
            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
        }

    }
}
