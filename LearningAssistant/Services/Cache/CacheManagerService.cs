using LearningAssistant.Common;
using LearningAssistant.Models.Cache;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace LearningAssistant.Services.Cache
{
    /// <summary>
    /// 缓存管理服务实现
    /// </summary>
    public class CacheManagerService : ICacheManagerService, IDisposable
    {
        private readonly ILogger<CacheManagerService>? _logger;
        private CacheCleanupConfig _config;
        private Timer? _cleanupTimer;
        private readonly object _lock = new();

        public DateTime? LastCleanupTime { get; private set; }

        public CacheManagerService(ILogger<CacheManagerService>? logger = null)
        {
            _logger = logger;
            _config = LoadConfig();

            if (_config.CleanupOnStartup)
            {
                Task.Run(() =>
                {
                    try
                    {
                        CleanupCache();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "启动时清理缓存失败");
                    }
                });
            }

            if (_config.AutoCleanupEnabled)
            {
                StartAutoCleanup();
            }
        }

        /// <inheritdoc/>
        public CacheCleanupResult CleanupCache()
        {
            var result = new CacheCleanupResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                var directories = GetCacheDirectories();

                foreach (var dir in directories)
                {
                    if (!dir.Enabled)
                        continue;

                    try
                    {
                        var expiryDays = dir.ExpiryDays > 0 ? dir.ExpiryDays : _config.CacheExpiryDays;
                        var dirResult = CleanupDirectoryInternal(dir.Path, expiryDays);
                        result.FilesCleared += dirResult.FilesCleared;
                        result.BytesFreed += dirResult.BytesFreed;
                        result.DirectoriesProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"清理目录 {dir.Name} 失败: {ex.Message}");
                        _logger?.LogError(ex, "清理缓存目录失败: {Dir}", dir.Name);
                    }
                }

                result.Success = true;
                LastCleanupTime = DateTime.Now;
                result.EndTime = DateTime.Now;

                _logger?.LogInformation(
                    $"缓存清理完成: 清理 {result.FilesCleared} 个文件, " +
                    $"释放 {result.SpaceFreedFormatted} 空间");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.Message);
                result.EndTime = DateTime.Now;
                _logger?.LogError(ex, "缓存清理失败");
            }

            return result;
        }

        /// <inheritdoc/>
        public long GetCacheSize()
        {
            try
            {
                long totalSize = 0;
                var directories = GetCacheDirectories();

                foreach (var dir in directories)
                {
                    if (!dir.Enabled || !Directory.Exists(dir.Path))
                        continue;

                    totalSize += GetDirectorySize(dir.Path);
                }

                return totalSize;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取缓存大小失败");
                return 0;
            }
        }

        /// <inheritdoc/>
        public string GetCacheSizeFormatted()
        {
            return FormatBytes(GetCacheSize());
        }

        /// <inheritdoc/>
        public Dictionary<string, long> GetDirectorySizes()
        {
            var result = new Dictionary<string, long>();
            try
            {
                var directories = GetCacheDirectories();

                foreach (var dir in directories)
                {
                    if (!Directory.Exists(dir.Path))
                    {
                        result[dir.Name] = 0;
                        continue;
                    }

                    result[dir.Name] = GetDirectorySize(dir.Path);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取目录大小失败");
            }

            return result;
        }

        /// <inheritdoc/>
        public CacheCleanupResult CleanupDirectory(string directoryPath, int expiryDays = 30)
        {
            var result = new CacheCleanupResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                var dirResult = CleanupDirectoryInternal(directoryPath, expiryDays);
                result.FilesCleared = dirResult.FilesCleared;
                result.BytesFreed = dirResult.BytesFreed;
                result.DirectoriesProcessed = 1;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.Message);
            }

            result.EndTime = DateTime.Now;
            return result;
        }

        /// <inheritdoc/>
        public CacheCleanupConfig GetConfig()
        {
            return _config;
        }

        /// <inheritdoc/>
        public void UpdateConfig(CacheCleanupConfig config)
        {
            lock (_lock)
            {
                _config = config;
                SaveConfig();

                if (config.AutoCleanupEnabled)
                {
                    StartAutoCleanup();
                }
                else
                {
                    StopAutoCleanup();
                }

                _logger?.LogInformation("缓存清理配置已更新");
            }
        }

        /// <inheritdoc/>
        public void ResetConfig()
        {
            _config = GetDefaultConfig();
            SaveConfig();
            _logger?.LogInformation("缓存清理配置已重置为默认");
        }

        /// <inheritdoc/>
        public void StartAutoCleanup()
        {
            lock (_lock)
            {
                StopAutoCleanup();

                if (_config.CleanupIntervalHours <= 0)
                    return;

                var interval = TimeSpan.FromHours(_config.CleanupIntervalHours).TotalMilliseconds;
                _cleanupTimer = new Timer(interval);
                _cleanupTimer.Elapsed += OnCleanupTimerElapsed;
                _cleanupTimer.AutoReset = true;
                _cleanupTimer.Start();

                _logger?.LogInformation($"自动清理已启动，间隔 {_config.CleanupIntervalHours} 小时");
            }
        }

        /// <inheritdoc/>
        public void StopAutoCleanup()
        {
            lock (_lock)
            {
                if (_cleanupTimer != null)
                {
                    _cleanupTimer.Stop();
                    _cleanupTimer.Dispose();
                    _cleanupTimer = null;
                }
            }
        }

        #region 私有方法

        private void OnCleanupTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                CleanupCache();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "定时清理缓存失败");
            }
        }

        private (int FilesCleared, long BytesFreed) CleanupDirectoryInternal(string directoryPath, int expiryDays)
        {
            int filesCleared = 0;
            long bytesFreed = 0;

            if (!Directory.Exists(directoryPath))
                return (filesCleared, bytesFreed);

            var cutoffDate = DateTime.Now.AddDays(-expiryDays);
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        var size = fileInfo.Length;
                        fileInfo.Delete();
                        filesCleared++;
                        bytesFreed += size;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "删除缓存文件失败: {File}", file);
                }
            }

            return (filesCleared, bytesFreed);
        }

        private long GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                return dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories)
                              .Sum(f => f.Length);
            }
            catch
            {
                return 0;
            }
        }

        private List<CacheDirectory> GetCacheDirectories()
        {
            if (_config.CacheDirectories.Count > 0)
                return _config.CacheDirectories;

            var defaultDirs = new List<CacheDirectory>
            {
                new() { Name = "临时文件", Path =Path.Combine( AppPaths.CacheDir, "temp"), Enabled = true, ExpiryDays = 7 },
                new() { Name = "PDF缓存", Path = Path.Combine(AppPaths.CacheDir, "pdf"), Enabled = true, ExpiryDays = 30 },
                new() { Name = "图片缓存", Path = Path.Combine(AppPaths.CacheDir, "images"), Enabled = true, ExpiryDays = 30 },
                new() { Name = "AI缓存", Path = Path.Combine(AppPaths.CacheDir, "ai"), Enabled = true, ExpiryDays = 15 },
                new() { Name = "缩略图缓存", Path = Path.Combine(AppPaths.CacheDir, "thumbnails"), Enabled = true, ExpiryDays = 60 }
            };

            _config.CacheDirectories = defaultDirs;
            return defaultDirs;
        }

        private CacheCleanupConfig LoadConfig()
        {
            try
            {
                var configPath = Path.Combine(AppPaths.ConfigDir, "CacheSettings.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<CacheCleanupConfig>(json);
                    if (config != null)
                        return config;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载缓存配置失败，使用默认配置");
            }

            return GetDefaultConfig();
        }

        private void SaveConfig()
        {
            try
            {
                var configPath = Path.Combine(AppPaths.ConfigDir, "CacheSettings.json");
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存缓存配置失败");
            }
        }

        private CacheCleanupConfig GetDefaultConfig()
        {
            return new CacheCleanupConfig
            {
                AutoCleanupEnabled = true,
                CacheExpiryDays = 30,
                CleanupIntervalHours = 24,
                MaxCacheSizeMB = 500,
                CleanupOnStartup = true
            };
        }

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

        public void Dispose()
        {
            StopAutoCleanup();
        }

        #endregion
    }
}
