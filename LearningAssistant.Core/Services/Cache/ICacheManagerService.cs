using LearningAssistant.Models.Cache;

namespace LearningAssistant.Services.Cache
{
    /// <summary>
    /// 缓存管理服务接口
    /// 提供缓存清理、统计、配置等功能
    /// </summary>
    public interface ICacheManagerService
    {
        /// <summary>
        /// 执行缓存清理
        /// </summary>
        /// <returns>清理结果</returns>
        CacheCleanupResult CleanupCache();

        /// <summary>
        /// 获取当前缓存大小
        /// </summary>
        /// <returns>缓存大小（字节）</returns>
        long GetCacheSize();

        /// <summary>
        /// 获取格式化的缓存大小
        /// </summary>
        string GetCacheSizeFormatted();

        /// <summary>
        /// 获取各目录的缓存大小
        /// </summary>
        Dictionary<string, long> GetDirectorySizes();

        /// <summary>
        /// 清理指定目录的缓存
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="expiryDays">过期天数</param>
        /// <returns>清理结果</returns>
        CacheCleanupResult CleanupDirectory(string directoryPath, int expiryDays = 30);

        /// <summary>
        /// 获取缓存清理配置
        /// </summary>
        CacheCleanupConfig GetConfig();

        /// <summary>
        /// 更新缓存清理配置
        /// </summary>
        void UpdateConfig(CacheCleanupConfig config);

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        void ResetConfig();

        /// <summary>
        /// 开始自动清理定时器
        /// </summary>
        void StartAutoCleanup();

        /// <summary>
        /// 停止自动清理定时器
        /// </summary>
        void StopAutoCleanup();

        /// <summary>
        /// 最后一次清理时间
        /// </summary>
        DateTime? LastCleanupTime { get; }
    }
}
