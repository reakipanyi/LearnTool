namespace UnifiedLearningAssistant.Services.Cloud
{
    /// <summary>
    /// 云存储服务接口，为第三方云存储（如百度网盘）提供统一访问方式
    /// </summary>
    public interface ICloudStorageService
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// 是否已授权
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 获取授权链接
        /// </summary>
        Task<string> GetAuthorizationUrlAsync();

        /// <summary>
        /// 授权回调处理
        /// </summary>
        Task<bool> AuthenticateAsync(string authCode);

        /// <summary>
        /// 从云存储下载文件
        /// </summary>
        /// <param name="cloudPath">云存储路径</param>
        /// <param name="localPath">本地保存路径</param>
        /// <param name="progress">下载进度回调（0-100）</param>
        Task<bool> DownloadFileAsync(string cloudPath, string localPath, Action<int>? progress = null);

        /// <summary>
        /// 上传文件到云存储
        /// </summary>
        /// <param name="localPath">本地文件路径</param>
        /// <param name="cloudPath">云存储路径</param>
        /// <param name="progress">上传进度回调（0-100）</param>
        Task<bool> UploadFileAsync(string localPath, string cloudPath, Action<int>? progress = null);

        /// <summary>
        /// 列出云存储文件
        /// </summary>
        /// <param name="cloudFolder">云存储文件夹路径</param>
        Task<List<CloudFileInfo>> ListFilesAsync(string cloudFolder);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        Task<bool> FileExistsAsync(string cloudPath);

        /// <summary>
        /// 删除云存储文件
        /// </summary>
        Task<bool> DeleteFileAsync(string cloudPath);

        /// <summary>
        /// 创建文件夹
        /// </summary>
        Task<bool> CreateFolderAsync(string cloudFolderPath);
    }

    /// <summary>
    /// 云存储文件信息
    /// </summary>
    public class CloudFileInfo
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "文件路径不能为空")]
        [System.ComponentModel.DataAnnotations.MaxLength(1000, ErrorMessage = "文件路径长度不能超过1000个字符")]
        public string Path { get; set; } = string.Empty;
        
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "文件名不能为空")]
        [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "文件名长度不能超过500个字符")]
        public string Name { get; set; } = string.Empty;
        
        [System.ComponentModel.DataAnnotations.Range(0, long.MaxValue, ErrorMessage = "文件大小不能为负数")]
        public long Size { get; set; }
        
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "修改时间不能为空")]
        public DateTime ModifiedTime { get; set; }
        
        public bool IsFolder { get; set; }
    }
}
