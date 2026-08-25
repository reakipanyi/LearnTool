namespace LearningAssistant.Models.Cache
{
    /// <summary>
    /// 缓存清理配置
    /// </summary>
    public class CacheCleanupConfig
    {
        /// <summary>
        /// 是否启用自动清理
        /// </summary>
        public bool AutoCleanupEnabled { get; set; } = true;

        /// <summary>
        /// 缓存过期天数（超过此天数的缓存会被清理）
        /// </summary>
        public int CacheExpiryDays { get; set; } = 30;

        /// <summary>
        /// 自动清理间隔（小时）
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;

        /// <summary>
        /// 最大缓存大小（MB）
        /// </summary>
        public long MaxCacheSizeMB { get; set; } = 500;

        /// <summary>
        /// 启动时自动清理
        /// </summary>
        public bool CleanupOnStartup { get; set; } = true;

        /// <summary>
        /// 要清理的缓存目录
        /// </summary>
        public List<CacheDirectory> CacheDirectories { get; set; } = new();
    }

    /// <summary>
    /// 缓存目录配置
    /// </summary>
    public class CacheDirectory
    {
        /// <summary>
        /// 目录路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 目录名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否在清理范围内
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 文件过期天数（单独配置，为0则使用全局配置）
        /// </summary>
        public int ExpiryDays { get; set; }
    }

    /// <summary>
    /// 缓存清理结果
    /// </summary>
    public class CacheCleanupResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 清理的文件数
        /// </summary>
        public int FilesCleared { get; set; }

        /// <summary>
        /// 释放的空间（字节）
        /// </summary>
        public long BytesFreed { get; set; }

        /// <summary>
        /// 释放的空间（MB，格式化后的字符串）
        /// </summary>
        public string SpaceFreedFormatted => FormatBytes(BytesFreed);

        /// <summary>
        /// 清理的目录数
        /// </summary>
        public int DirectoriesProcessed { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 清理开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 清理结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 耗时（毫秒）
        /// </summary>
        public double DurationMs => (EndTime - StartTime).TotalMilliseconds;

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
