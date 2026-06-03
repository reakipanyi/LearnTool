using Microsoft.Extensions.Logging;

namespace UnifiedLearningAssistant.Services.Cloud
{
    /// <summary>
    /// 云存储服务占位符实现，等待具体集成（如百度网盘）
    /// </summary>
    public class PlaceholderCloudStorageService : ICloudStorageService
    {
        private readonly ILogger<PlaceholderCloudStorageService>? _logger;

        public string ServiceName => "占位符云存储服务";
        public bool IsAuthenticated => false;

        public PlaceholderCloudStorageService(ILogger<PlaceholderCloudStorageService>? logger = null)
        {
            _logger = logger;
        }

        public Task<string> GetAuthorizationUrlAsync()
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<bool> AuthenticateAsync(string authCode)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<bool> DownloadFileAsync(string cloudPath, string localPath, Action<int>? progress = null)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<bool> UploadFileAsync(string localPath, string cloudPath, Action<int>? progress = null)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<List<CloudFileInfo>> ListFilesAsync(string cloudFolder)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<bool> FileExistsAsync(string cloudPath)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<bool> DeleteFileAsync(string cloudPath)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }

        public Task<bool> CreateFolderAsync(string cloudFolderPath)
        {
            _logger?.LogWarning("PlaceholderCloudStorageService: 此服务尚未实现具体的云存储集成");
            throw new NotImplementedException("云存储服务尚未集成，请等待具体实现（如百度网盘）");
        }
    }
}
