namespace LearningAssistant.Services.Cloud
{
    /// <summary>
    /// 云存储服务接口 - 为第三方云存储（如百度网盘）提供统一访问方式
    /// </summary>
    public interface ICloudStorageService
    {
        /// <summary>
        /// 云存储服务名称
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// 是否已授权（用户已完成OAuth授权流程）
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 是否已配置（已设置必要的API凭证）
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// 获取授权URL
        /// 用于引导用户到云存储服务进行授权
        /// </summary>
        /// <returns>授权页面URL</returns>
        Task<string> GetAuthorizationUrlAsync();

        /// <summary>
        /// 处理授权回调
        /// 用户从云存储服务授权后会携带auth code回调到此方法
        /// </summary>
        /// <param name="authCode">授权码</param>
        /// <returns>授权成功返回true</returns>
        Task<bool> AuthenticateAsync(string authCode);

        /// <summary>
        /// 从云存储下载文件到本地
        /// </summary>
        /// <param name="cloudPath">云存储上的文件路径</param>
        /// <param name="localPath">本地保存路径</param>
        /// <param name="progress">进度回调，参数为百分比（0-100）</param>
        /// <returns>下载成功返回true</returns>
        Task<bool> DownloadFileAsync(string cloudPath, string localPath, Action<int>? progress = null);

        /// <summary>
        /// 上传本地文件到云存储
        /// </summary>
        /// <param name="localPath">本地文件路径</param>
        /// <param name="cloudPath">云存储上的目标路径</param>
        /// <param name="progress">进度回调，参数为百分比（0-100）</param>
        /// <returns>上传成功返回true</returns>
        Task<bool> UploadFileAsync(string localPath, string cloudPath, Action<int>? progress = null);

        /// <summary>
        /// 列出云存储文件夹中的文件
        /// </summary>
        /// <param name="cloudFolder">云存储文件夹路径</param>
        /// <returns>文件信息列表</returns>
        Task<List<CloudFileInfo>> ListFilesAsync(string cloudFolder);

        /// <summary>
        /// 检查文件是否存在于云存储
        /// </summary>
        /// <param name="cloudPath">云存储文件路径</param>
        /// <returns>存在返回true</returns>
        Task<bool> FileExistsAsync(string cloudPath);

        /// <summary>
        /// 删除云存储上的文件
        /// </summary>
        /// <param name="cloudPath">云存储文件路径</param>
        /// <returns>删除成功返回true</returns>
        Task<bool> DeleteFileAsync(string cloudPath);

        /// <summary>
        /// 在云存储上创建文件夹
        /// </summary>
        /// <param name="cloudFolderPath">云存储文件夹路径</param>
        /// <returns>创建成功返回true</returns>
        Task<bool> CreateFolderAsync(string cloudFolderPath);
    }

    /// <summary>
    /// 云存储文件信息 - 包含文件的基本属性
    /// </summary>
    public class CloudFileInfo
    {
        /// <summary>
        /// 文件完整路径
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "文件路径不能为空")]
        [System.ComponentModel.DataAnnotations.MaxLength(1000, ErrorMessage = "文件路径长度不能超过1000个字符")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "文件名不能为空")]
        [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "文件名长度不能超过500个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        [System.ComponentModel.DataAnnotations.Range(0, long.MaxValue, ErrorMessage = "文件大小不能为负数")]
        public long Size { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "修改时间不能为空")]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 是否为文件夹
        /// </summary>
        public bool IsFolder { get; set; }
    }
}
