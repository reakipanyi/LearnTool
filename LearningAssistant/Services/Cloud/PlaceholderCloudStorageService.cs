using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Cloud
{
    /// <summary>
    /// 云存储服务占位符实现，等待具体集成（如百度网盘）
    /// </summary>
    public class PlaceholderCloudStorageService : ICloudStorageService
    {
        private readonly ILogger<PlaceholderCloudStorageService>? _logger;
        private const string NotImplMessage = "云存储服务尚未集成，请等待具体实现（如百度网盘）";

        public string ServiceName => "占位符云存储服务";
        public bool IsAuthenticated => false;
        public bool IsConfigured => false;

        public PlaceholderCloudStorageService(ILogger<PlaceholderCloudStorageService>? logger = null)
        {
            _logger = logger;
        }

        public Task<string> GetAuthorizationUrlAsync()
        {
            return ThrowNotImplemented<string>();
        }

        public Task<bool> AuthenticateAsync(string authCode)
        {
            return ThrowNotImplemented<bool>();
        }

        public Task<bool> DownloadFileAsync(string cloudPath, string localPath, Action<int>? progress = null)
        {
            return ThrowNotImplemented<bool>();
        }

        public Task<bool> UploadFileAsync(string localPath, string cloudPath, Action<int>? progress = null)
        {
            return ThrowNotImplemented<bool>();
        }

        public Task<List<CloudFileInfo>> ListFilesAsync(string cloudFolder)
        {
            return ThrowNotImplemented<List<CloudFileInfo>>();
        }

        public Task<bool> FileExistsAsync(string cloudPath)
        {
            return ThrowNotImplemented<bool>();
        }

        public Task<bool> DeleteFileAsync(string cloudPath)
        {
            return ThrowNotImplemented<bool>();
        }

        public Task<bool> CreateFolderAsync(string cloudFolderPath)
        {
            return ThrowNotImplemented<bool>();
        }

        private Task<T> ThrowNotImplemented<T>()
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException(NotImplMessage);
        }

        private Task ThrowNotImplemented()
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException(NotImplMessage);
        }
    }
}
