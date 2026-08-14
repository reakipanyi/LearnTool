using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Baidu
{
    /// <summary>
    /// 百度网盘智能管理工具（已排除）
    /// 主要功能：批量重命名、目录标准化、文件统计、CSV导出
    /// </summary>
    public class BaiduPanSmartTool : IDisposable
    {
        #region 配置类

        /// <summary>
        /// 工具配置（所有可调参数集中管理）
        /// </summary>
        public class ToolConfig
        {
            /// <summary>网盘根目录</summary>
            public string PanRootPath { get; set; } = "/";

            /// <summary>每秒最大请求数</summary>
            public int RequestsPerSecond { get; set; } = 3;

            /// <summary>是否包含子目录</summary>
            public bool IncludeSubdirectories { get; set; } = true;

            /// <summary>集数处理模式</summary>
            public EpisodeHandleMode EpisodeMode { get; set; } = EpisodeHandleMode.Standardize;

            /// <summary>集数格式化模板</summary>
            public string EpisodeFormatTemplate { get; set; } = "EP{0:00}";

            /// <summary>是否清理随机字符</summary>
            public bool CleanRandomChars { get; set; } = true;

            /// <summary>随机字符最小长度</summary>
            public int RandomCharMinLength { get; set; } = 8;

            /// <summary>是否强制重新处理</summary>
            public bool ForceReprocess { get; set; } = false;

            /// <summary>是否启用进度日志</summary>
            public bool EnableProgressLogging { get; set; } = true;

            /// <summary>固定替换字典</summary>
            public Dictionary<string, string> FixedReplacements { get; set; } = new();

            /// <summary>英文白名单</summary>
            public HashSet<string> EnglishWhitelist { get; set; } = new(StringComparer.OrdinalIgnoreCase)
            {
                "HD", "4K", "8K", "VIP", "PRO", "MAX", "MIN", "LITE",
                "SEASON", "EP", "PART", "VOL", "VERSION", "FINAL",
                "EDIT", "CUT", "RAW", "REMUX", "WEB", "TV", "MOVIE",
                "DOC", "MP4", "MKV", "AVI", "FLV", "WMV", "MP3",
                "FLAC", "WAV", "TEST", "DEMO", "SAMPLE"
            };

            /// <summary>处理记录文件路径</summary>
            public string ProcessRecordFilePath { get; set; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BaiduPanTools",
                "rename_records.json");

            /// <summary>目录处理记录文件路径</summary>
            public string DirProcessRecordFilePath { get; set; } = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BaiduPanTools",
                "dir_records.json");
        }

        /// <summary>
        /// 集数处理模式
        /// </summary>
        public enum EpisodeHandleMode
        {
            None,           // 不处理
            Standardize,    // 标准化为统一格式
            Remove          // 移除集数信息
        }

        #endregion

        #region 私有字段

        private readonly BaiduPanApiClient _panClient;
        private readonly TokenBucketRateLimiter _rateLimiter;
        private readonly IMemoryCache _dirCache;
        private readonly ConcurrentDictionary<string, ConcurrentBag<RenameItem>> _renameQueue;
        private readonly ConcurrentDictionary<string, byte> _processedDirs;
        private readonly SemaphoreSlim _recordLock = new(1, 1);
        private readonly ToolConfig _config;
        private readonly List<FileProcessRecord> _pendingRecords = new();
        private readonly System.Threading.Timer _batchSaveTimer;
        private bool _disposed;
        private int _isBatchSaving;

        // 重试配置
        private const int MaxRetryCount = 3;
        private const int InitialRetryDelayMs = 500;
        private const int MaxRetryDelayMs = 30000;

        // 批量保存配置
        private const int BatchSaveIntervalMs = 10000;
        private const int BatchSaveThreshold = 50;

        // 缓存配置
        private const int DirCacheSizeLimit = 200;
        private const int DirCacheExpirationMinutes = 30;

        #endregion

        #region 构造函数与资源释放

        public BaiduPanSmartTool(BaiduPanApiClient panClient, ToolConfig config = null)
        {
            _panClient = panClient ?? throw new ArgumentNullException(nameof(panClient));
            _config = config ?? new ToolConfig();

            // 确保记录目录存在
            EnsureDirectoryExists(_config.ProcessRecordFilePath);
            EnsureDirectoryExists(_config.DirProcessRecordFilePath);

            _rateLimiter = new TokenBucketRateLimiter(_config.RequestsPerSecond);
            _dirCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = DirCacheSizeLimit,
                ExpirationScanFrequency = TimeSpan.FromMinutes(5)
            });
            _renameQueue = new ConcurrentDictionary<string, ConcurrentBag<RenameItem>>();
            _processedDirs = new ConcurrentDictionary<string, byte>();

            _batchSaveTimer = new System.Threading.Timer(
                _ => _ = BatchSaveRecordsAsync(),
                null,
                BatchSaveIntervalMs,
                BatchSaveIntervalMs);

            LogInfo($"百度网盘智能工具初始化完成，根目录：{_config.PanRootPath}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _batchSaveTimer?.Dispose();
            BatchSaveRecordsAsync(force: true).ConfigureAwait(false).GetAwaiter().GetResult();
            _recordLock?.Dispose();
            _rateLimiter?.Dispose();
            _dirCache?.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region 限流器实现（安全版）

        /// <summary>
        /// 基于令牌桶的安全限流器
        /// </summary>
        private class TokenBucketRateLimiter : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private readonly System.Threading.Timer _timer;
            private readonly int _maxTokens;
            private int _currentTokens;
            private readonly object _lock = new();
            private bool _disposed;

            public TokenBucketRateLimiter(int maxTokens)
            {
                _maxTokens = Math.Max(1, maxTokens);
                _currentTokens = _maxTokens;
                _semaphore = new SemaphoreSlim(_currentTokens, _maxTokens);

                var interval = TimeSpan.FromSeconds(1.0 / _maxTokens);
                _timer = new System.Threading.Timer(ReleaseToken, null, TimeSpan.Zero, interval);
            }

            private void ReleaseToken(object state)
            {
                if (_disposed) return;
                lock (_lock)
                {
                    if (_currentTokens < _maxTokens)
                    {
                        _currentTokens++;
                        try { _semaphore.Release(); }
                        catch (SemaphoreFullException) { /* 信号量已满，忽略 */ }
                    }
                }
            }

            public async Task WaitAsync(CancellationToken cancellationToken = default)
            {
                await _semaphore.WaitAsync(cancellationToken);
                lock (_lock)
                {
                    _currentTokens--;
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _timer?.Dispose();
                _semaphore?.Dispose();
            }
        }

        #endregion

        #region 重命名项定义

        /// <summary>
        /// 重命名项（统一表示文件和目录的重命名操作）
        /// </summary>
        private class RenameItem
        {
            public string OldPath { get; set; }
            public string NewPath { get; set; }
            public bool IsDirectory { get; set; }

            public string NewName => System.IO.Path.GetFileName(NewPath);
            public string ParentDir => GetParentDirectory(OldPath);
        }

        #endregion

        #region 记录类定义

        private class FileProcessRecord
        {
            public long FsId { get; set; }
            public string OriginalPath { get; set; }
            public string NewPath { get; set; }
            public DateTime ProcessTime { get; set; }
            public string StandardName { get; set; }
        }

        private class DirProcessRecord
        {
            public string Path { get; set; }
            public DateTime ProcessTime { get; set; }
        }

        #endregion

        #region 统计类定义

        public class FolderStatistics
        {
            public string FolderPath { get; set; }
            public long TotalFileCount { get; set; }
            public long TotalSize { get; set; }
            public Dictionary<string, FileTypeStat> FileTypes { get; set; } = new();
        }

        public class FileTypeStat
        {
            public string Extension { get; set; }
            public long Count { get; set; }
            public long TotalSize { get; set; }
        }

        public class FolderStatisticsNode
        {
            public string Path { get; set; }
            public long FileCount { get; set; }
            public long TotalSize { get; set; }
            public Dictionary<string, FileTypeStat> FileTypes { get; set; } = new();
            public List<FolderStatisticsNode> SubFolders { get; set; } = new();

            public long TotalFileCount => FileCount + SubFolders.Sum(f => f.TotalFileCount);
            public long TotalSizeRecursive => TotalSize + SubFolders.Sum(f => f.TotalSizeRecursive);
        }

        #endregion

        #region 核心方法：目录分析重命名

        /// <summary>
        /// 分析并重命名目录（根据内容类型添加后缀）
        /// </summary>
        public async Task AnalyzeAndRenameDirectoriesAsync(CancellationToken cancellationToken = default)
        {
            await LoadDirProcessRecordsAsync(cancellationToken);
            LogInfo($"开始分析目录：{_config.PanRootPath}");

            var allDirs = new List<string> { _config.PanRootPath };
            var index = 0;

            while (index < allDirs.Count && !cancellationToken.IsCancellationRequested)
            {
                var currentDir = allDirs[index++];

                if (!_config.ForceReprocess && _processedDirs.ContainsKey(currentDir))
                {
                    LogDebug($"跳过已处理目录：{currentDir}");
                    continue;
                }

                var entries = await GetDirectoryEntriesWithCacheAsync(currentDir, cancellationToken);
                if (entries == null || entries.Count == 0) continue;

                var files = entries.Where(e => e.IsDir == 0).ToList();
                var subDirs = entries.Where(e => e.IsDir == 1).ToList();

                foreach (var subDir in subDirs)
                {
                    if (!allDirs.Contains(subDir.Path))
                        allDirs.Add(subDir.Path);
                }

                // 判断目录类型
                var typeSuffix = DetermineDirectoryType(files);
                if (typeSuffix != null)
                {
                    var newDirName = BuildStandardDirectoryName(currentDir, typeSuffix, files.Count);
                    var currentDirName = System.IO.Path.GetFileName(currentDir);

                    if (!string.Equals(currentDirName, newDirName, StringComparison.OrdinalIgnoreCase))
                    {
                        var parentDir = GetParentDirectory(currentDir);
                        var newPath = CombinePath(parentDir, newDirName);
                        AddRenameItem(new RenameItem
                        {
                            OldPath = currentDir,
                            NewPath = newPath,
                            IsDirectory = true
                        });
                    }
                }

                // 标记已处理
                _processedDirs.TryAdd(currentDir, 0);
            }

            // 执行批量重命名
            await ExecuteBatchRenamesAsync(cancellationToken);
            await SaveDirProcessRecordsAsync(cancellationToken);
            LogInfo("目录分析重命名完成");
        }

        /// <summary>
        /// 根据文件类型分布判断目录后缀
        /// </summary>
        private string DetermineDirectoryType(List<BaseFileInfo> files)
        {
            if (files == null || files.Count == 0) return null;

            var categories = files
                .GroupBy(f => (FileCategory)f.Category)
                .Where(g => g.Any())
                .ToList();

            if (categories.Count != 1) return null;

            return categories[0].Key switch
            {
                FileCategory.Audio => "音频",
                FileCategory.Video => "视频",
                FileCategory.Image => "图片",
                FileCategory.Document => "文档",
                _ => null
            };
        }

        /// <summary>
        /// 构建标准目录名
        /// </summary>
        private string BuildStandardDirectoryName(string currentPath, string typeSuffix, int fileCount)
        {
            var baseName = System.IO.Path.GetFileName(currentPath);

            // 清理旧的标记
            baseName = Regex.Replace(baseName, @"[\(（]\d+集(?:全)?[\)）]", "");
            baseName = baseName.Replace("【音频】", "").Replace("【视频】", "")
                .Replace("【图片】", "").Replace("【文档】", "");
            baseName = Regex.Replace(baseName, @"[\(（]\d+[张个集][\)）]", "");
            baseName = baseName.Trim();

            var unit = typeSuffix == "图片" ? "张" : "个";
            if (!baseName.Contains(fileCount.ToString()) && !baseName.Contains("课") && !baseName.Contains("集"))
            {
                return $"{baseName}({fileCount}{unit})【{typeSuffix}】";
            }
            return $"{baseName}【{typeSuffix}】";
        }

        #endregion

        #region 核心方法：文件重命名

        /// <summary>
        /// 标准化文件名称
        /// </summary>
        public async Task RenameFilesAsync(CancellationToken cancellationToken = default)
        {
            var processedRecords = await LoadProcessRecordsAsync(cancellationToken);
            LogInfo($"已加载历史记录：{processedRecords.Count} 个文件");
            LogInfo($"开始处理：{_config.PanRootPath}（{(_config.IncludeSubdirectories ? "包含" : "不包含")}子目录）");

            var stats = (Total: 0, Skipped: 0, Processed: 0, Failed: 0);
            var start = 0;
            const int limit = 1000;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _panClient.GetFileListRecursiveAsync(
                    path: _config.PanRootPath,
                    recursion: _config.IncludeSubdirectories ? 1 : 0,
                    order: "size",
                    desc: 1,
                    start: start,
                    limit: limit);

                if (response.ErrorCode != 0 || response.FileList == null || response.FileList.Count == 0)
                    break;

                stats.Total += response.FileList.Count;
                LogInfo($"分页 {start / limit + 1}：获取 {response.FileList.Count} 个文件");

                foreach (var file in response.FileList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_config.ForceReprocess && IsFileProcessed(file, processedRecords))
                    {
                        stats.Skipped++;
                        continue;
                    }

                    try
                    {
                        var standardName = GetStandardFileName(file.ServerFileName);
                        var extension = System.IO.Path.GetExtension(file.ServerFileName);
                        var parentDir = GetParentDirectory(file.Path);
                        var targetFileName = $"{standardName}{extension}";
                        var targetPath = CombinePath(parentDir, targetFileName);

                        if (string.Equals(file.Path, targetPath, StringComparison.OrdinalIgnoreCase))
                        {
                            // 文件名已符合标准，仅记录
                            AddPendingRecord(new FileProcessRecord
                            {
                                FsId = file.FsId,
                                OriginalPath = file.Path,
                                NewPath = targetPath,
                                ProcessTime = DateTime.UtcNow,
                                StandardName = standardName
                            });
                            stats.Processed++;
                            continue;
                        }

                        await RenameFileAsync(file, targetFileName, cancellationToken);

                        AddPendingRecord(new FileProcessRecord
                        {
                            FsId = file.FsId,
                            OriginalPath = file.Path,
                            NewPath = targetPath,
                            ProcessTime = DateTime.UtcNow,
                            StandardName = standardName
                        });
                        stats.Processed++;
                    }
                    catch (Exception ex)
                    {
                        stats.Failed++;
                        LogError($"处理失败：{file.ServerFileName}，错误：{ex.Message}");
                    }
                }

                if (response.FileList.Count < limit) break;
                start += limit;
                await Task.Delay(1000, cancellationToken);

            } while (true);

            // 强制保存剩余记录
            await BatchSaveRecordsAsync(force: true);

            LogInfo($"\n===== 处理完成 =====");
            LogInfo($"总文件数：{stats.Total}");
            LogInfo($"已跳过：{stats.Skipped}");
            LogInfo($"处理成功：{stats.Processed}");
            LogInfo($"处理失败：{stats.Failed}");
        }

        #endregion

        #region 辅助方法：文件重命名

        /// <summary>
        /// 重命名单个文件
        /// </summary>
        private async Task RenameFileAsync(BaseFileInfo file, string targetFileName, CancellationToken cancellationToken)
        {
            var items = new List<FileManagerFileItem>
            {
                new() { Path = file.Path, NewName = targetFileName }
            };

            await _rateLimiter.WaitAsync(cancellationToken);

            var response = await RetryAsync(async () =>
                await _panClient.ManageFileAsync(
                    opera: FileOperation.Rename,
                    fileList: items,
                    async: 1,
                    onDup: OnDupStrategy.NewCopy),
                cancellationToken);

            if (response.ErrorCode == 0)
            {
                LogInfo($"重命名成功：{file.ServerFileName} → {targetFileName}");
            }
            else
            {
                throw new Exception($"重命名失败，错误码：{response.ErrorCode}");
            }
        }

        /// <summary>
        /// 获取标准化文件名（不含扩展名）
        /// </summary>
        private string GetStandardFileName(string originalFileName)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(originalFileName);

            // 应用固定替换
            foreach (var kv in _config.FixedReplacements)
            {
                name = name.Replace(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase);
            }

            // 处理集数
            if (_config.EpisodeMode == EpisodeHandleMode.Standardize)
                name = StandardizeEpisodeNumber(name);
            else if (_config.EpisodeMode == EpisodeHandleMode.Remove)
                name = RemoveEpisodeNumber(name);

            // 清理随机字符
            if (_config.CleanRandomChars)
                name = CleanRandomCharsSafe(name);

            return string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileNameWithoutExtension(originalFileName) : name.Trim();
        }

        /// <summary>
        /// 标准化集数格式
        /// </summary>
        private string StandardizeEpisodeNumber(string fileName)
        {
            var patterns = new (string Pattern, Func<Match, string> Replacement)[]
            {
                (@"第(\d+)集", m => string.Format(_config.EpisodeFormatTemplate, int.Parse(m.Groups[1].Value))),
                (@"EP(\d+)", m => string.Format(_config.EpisodeFormatTemplate, int.Parse(m.Groups[1].Value))),
                (@"E(\d+)", m => string.Format(_config.EpisodeFormatTemplate, int.Parse(m.Groups[1].Value))),
                (@"(\d+)集", m => string.Format(_config.EpisodeFormatTemplate, int.Parse(m.Groups[1].Value))),
            };

            foreach (var (pattern, replacement) in patterns)
            {
                var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return Regex.Replace(fileName, pattern, replacement(match), RegexOptions.IgnoreCase);
                }
            }

            return fileName;
        }

        /// <summary>
        /// 移除集数信息
        /// </summary>
        private string RemoveEpisodeNumber(string fileName)
        {
            var result = Regex.Replace(fileName, @"第\d+集", "");
            result = Regex.Replace(result, @"EP\d+", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"E\d+", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\d+集", "");
            return result.Trim();
        }

        /// <summary>
        /// 安全地清理随机字符（避免误删）
        /// </summary>
        private string CleanRandomCharsSafe(string fileName)
        {
            // 先分割成片段
            var parts = fileName.Split(new[] { '.', '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var cleanedParts = parts.Where(part =>
            {
                // 白名单直接保留
                if (_config.EnglishWhitelist.Contains(part))
                    return true;

                // 纯数字保留（可能是集数）
                if (int.TryParse(part, out _))
                    return true;

                // 长度小于阈值保留
                if (part.Length < _config.RandomCharMinLength)
                    return true;

                // 判断是否像随机字符串（包含混合的大小写字母和数字）
                var hasUpper = part.Any(char.IsUpper);
                var hasLower = part.Any(char.IsLower);
                var hasDigit = part.Any(char.IsDigit);

                // 如果同时包含大小写和数字，且长度超过阈值，可能是随机广告
                if (hasUpper && hasLower && hasDigit && part.Length >= _config.RandomCharMinLength)
                    return false;

                // 如果全是小写+数字，但长度很长，也可能是随机
                if (!hasUpper && hasLower && hasDigit && part.Length >= _config.RandomCharMinLength + 4)
                    return false;

                return true;
            });

            return string.Join(".", cleanedParts);
        }

        #endregion

        #region 批量重命名执行

        /// <summary>
        /// 添加重命名项到队列
        /// </summary>
        private void AddRenameItem(RenameItem item)
        {
            var key = item.IsDirectory ? $"dir_{item.ParentDir}" : $"file_{item.ParentDir}";
            _renameQueue.AddOrUpdate(key,
                _ => new ConcurrentBag<RenameItem> { item },
                (_, bag) => { bag.Add(item); return bag; });
        }

        /// <summary>
        /// 执行批量重命名
        /// </summary>
        private async Task ExecuteBatchRenamesAsync(CancellationToken cancellationToken)
        {
            const int batchSize = 500;

            foreach (var kv in _renameQueue)
            {
                var items = kv.Value.ToList();
                if (items.Count == 0) continue;

                LogInfo($"批量重命名 {items.Count} 个条目");

                for (int i = 0; i < items.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = items.Skip(i).Take(batchSize).ToList();
                    var fileItems = batch.Select(item => new FileManagerFileItem
                    {
                        Path = item.OldPath,
                        NewName = item.NewName
                    }).ToList();

                    await _rateLimiter.WaitAsync(cancellationToken);

                    var response = await RetryAsync(async () =>
                        await _panClient.ManageFileAsync(
                            opera: FileOperation.Rename,
                            fileList: fileItems,
                            async: 1,
                            onDup: OnDupStrategy.NewCopy),
                        cancellationToken);

                    if (response.ErrorCode == 0)
                    {
                        LogInfo($"批量重命名成功 {batch.Count} 个条目");
                    }
                    else
                    {
                        LogError($"批量重命名失败，错误码：{response.ErrorCode}");
                    }

                    await Task.Delay(500, cancellationToken);
                }
            }

            _renameQueue.Clear();
        }

        #endregion

        #region 目录缓存与API调用

        /// <summary>
        /// 获取目录条目（带缓存）
        /// </summary>
        private async Task<List<BaseFileInfo>> GetDirectoryEntriesWithCacheAsync(string dir, CancellationToken cancellationToken)
        {
            if (_dirCache.TryGetValue(dir, out List<BaseFileInfo> cached))
                return cached;

            var entries = await GetAllEntriesAsync(dir, cancellationToken);
            var options = new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration = TimeSpan.FromMinutes(DirCacheExpirationMinutes)
            };
            _dirCache.Set(dir, entries ?? new List<BaseFileInfo>(), options);
            return entries ?? new List<BaseFileInfo>();
        }

        /// <summary>
        /// 获取目录下所有条目（分页）
        /// </summary>
        private async Task<List<BaseFileInfo>> GetAllEntriesAsync(string dir, CancellationToken cancellationToken)
        {
            var result = new List<BaseFileInfo>();
            int start = 0;
            const int pageSize = 1000;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _rateLimiter.WaitAsync(cancellationToken);

                var response = await RetryAsync(async () =>
                    await _panClient.GetFileListAsync(dir, "name", 1, start, pageSize),
                    cancellationToken);

                if (response.ErrorCode != 0 || response.FileList == null || response.FileList.Count == 0)
                    break;

                result.AddRange(response.FileList);

                if (response.FileList.Count < pageSize) break;
                start += pageSize;
                await Task.Delay(100, cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// 带重试的异步操作执行器
        /// </summary>
        private async Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            int delayMs = InitialRetryDelayMs;

            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (retryCount < MaxRetryCount)
                {
                    retryCount++;
                    var isRateLimit = ex.Message.Contains("20012") || ex.Message.Contains("访问超限");

                    if (isRateLimit)
                    {
                        delayMs = Math.Min(delayMs * 2, MaxRetryDelayMs);
                        LogDebug($"检测到限流，等待 {delayMs}ms 后重试...");
                    }
                    else
                    {
                        LogDebug($"操作失败，第 {retryCount}/{MaxRetryCount} 次重试：{ex.Message}");
                        delayMs = Math.Min(delayMs * 2, 5000);
                    }

                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        #endregion

        #region 记录持久化

        /// <summary>
        /// 加载文件处理记录
        /// </summary>
        private async Task<List<FileProcessRecord>> LoadProcessRecordsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_config.ProcessRecordFilePath))
                    return new List<FileProcessRecord>();

                var json = await File.ReadAllTextAsync(_config.ProcessRecordFilePath, cancellationToken);
                return JsonConvert.DeserializeObject<List<FileProcessRecord>>(json) ?? new List<FileProcessRecord>();
            }
            catch
            {
                return new List<FileProcessRecord>();
            }
        }

        /// <summary>
        /// 保存文件处理记录
        /// </summary>
        private async Task SaveProcessRecordsAsync(List<FileProcessRecord> records, CancellationToken cancellationToken)
        {
            await _recordLock.WaitAsync(cancellationToken);
            try
            {
                EnsureDirectoryExists(_config.ProcessRecordFilePath);
                var json = JsonConvert.SerializeObject(records, Formatting.Indented);
                await File.WriteAllTextAsync(_config.ProcessRecordFilePath, json, Encoding.UTF8, cancellationToken);
            }
            finally
            {
                _recordLock.Release();
            }
        }

        /// <summary>
        /// 添加待保存记录（批量缓冲）
        /// </summary>
        private void AddPendingRecord(FileProcessRecord record)
        {
            lock (_pendingRecords)
            {
                _pendingRecords.Add(record);
                if (_pendingRecords.Count >= BatchSaveThreshold)
                {
                    _ = BatchSaveRecordsAsync();
                }
            }
        }

        /// <summary>
        /// 批量保存记录
        /// </summary>
        private async Task BatchSaveRecordsAsync(bool force = false)
        {
            if (Interlocked.Exchange(ref _isBatchSaving, 1) == 1)
                return;

            try
            {
                List<FileProcessRecord> recordsToSave = null;
                lock (_pendingRecords)
                {
                    if (_pendingRecords.Count == 0) return;
                    if (force || _pendingRecords.Count >= BatchSaveThreshold)
                    {
                        recordsToSave = new List<FileProcessRecord>(_pendingRecords);
                        _pendingRecords.Clear();
                    }
                }

                if (recordsToSave != null && recordsToSave.Count > 0)
                {
                    var existing = await LoadProcessRecordsAsync(CancellationToken.None);
                    existing.AddRange(recordsToSave);
                    await SaveProcessRecordsAsync(existing, CancellationToken.None);
                    LogDebug($"批量保存 {recordsToSave.Count} 条记录");
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isBatchSaving, 0);
            }
        }

        /// <summary>
        /// 判断文件是否已处理
        /// </summary>
        private bool IsFileProcessed(BaseFileInfo file, List<FileProcessRecord> records)
        {
            return records.Any(r =>
                r.FsId == file.FsId ||
                string.Equals(r.OriginalPath, file.Path, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.NewPath, file.Path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 加载目录处理记录
        /// </summary>
        private async Task LoadDirProcessRecordsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(_config.DirProcessRecordFilePath)) return;
                var json = await File.ReadAllTextAsync(_config.DirProcessRecordFilePath, cancellationToken);
                var records = JsonConvert.DeserializeObject<List<DirProcessRecord>>(json);
                if (records != null)
                {
                    foreach (var r in records)
                        _processedDirs.TryAdd(r.Path, 0);
                }
            }
            catch { }
        }

        /// <summary>
        /// 保存目录处理记录
        /// </summary>
        private async Task SaveDirProcessRecordsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var records = _processedDirs.Keys
                    .Select(p => new DirProcessRecord { Path = p, ProcessTime = DateTime.UtcNow })
                    .ToList();

                EnsureDirectoryExists(_config.DirProcessRecordFilePath);
                var json = JsonConvert.SerializeObject(records, Formatting.Indented);
                await File.WriteAllTextAsync(_config.DirProcessRecordFilePath, json, Encoding.UTF8, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError($"保存目录记录失败：{ex.Message}");
            }
        }

        #endregion

        #region 文件夹统计

        /// <summary>
        /// 获取文件夹统计信息
        /// </summary>
        public async Task<FolderStatistics> GetFolderStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new FolderStatistics { FolderPath = _config.PanRootPath };
            await TraverseFolderForStatsAsync(_config.PanRootPath, stats, cancellationToken);
            return stats;
        }

        private async Task TraverseFolderForStatsAsync(string folderPath, FolderStatistics stats, CancellationToken cancellationToken)
        {
            var entries = await GetDirectoryEntriesWithCacheAsync(folderPath, cancellationToken);
            if (entries == null) return;

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDir == 1)
                {
                    await TraverseFolderForStatsAsync(entry.Path, stats, cancellationToken);
                }
                else
                {
                    stats.TotalFileCount++;
                    stats.TotalSize += entry.Size;

                    var ext = System.IO.Path.GetExtension(entry.ServerFileName)?.ToLowerInvariant() ?? "(无扩展名)";
                    if (!stats.FileTypes.TryGetValue(ext, out var typeStat))
                    {
                        typeStat = new FileTypeStat { Extension = ext };
                        stats.FileTypes[ext] = typeStat;
                    }
                    typeStat.Count++;
                    typeStat.TotalSize += entry.Size;
                }
            }
        }

        /// <summary>
        /// 获取详细树形统计
        /// </summary>
        public async Task<FolderStatisticsNode> GetDetailedStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await TraverseFolderDetailedAsync(_config.PanRootPath, cancellationToken);
        }

        private async Task<FolderStatisticsNode> TraverseFolderDetailedAsync(string folderPath, CancellationToken cancellationToken)
        {
            var entries = await GetDirectoryEntriesWithCacheAsync(folderPath, cancellationToken);
            var node = new FolderStatisticsNode { Path = folderPath };

            if (entries == null) return node;

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDir == 1)
                {
                    node.SubFolders.Add(await TraverseFolderDetailedAsync(entry.Path, cancellationToken));
                }
                else
                {
                    node.FileCount++;
                    node.TotalSize += entry.Size;

                    var ext = System.IO.Path.GetExtension(entry.ServerFileName)?.ToLowerInvariant() ?? "(无扩展名)";
                    if (!node.FileTypes.TryGetValue(ext, out var typeStat))
                    {
                        typeStat = new FileTypeStat { Extension = ext };
                        node.FileTypes[ext] = typeStat;
                    }
                    typeStat.Count++;
                    typeStat.TotalSize += entry.Size;
                }
            }

            return node;
        }

        /// <summary>
        /// 保存统计结果到文件
        /// </summary>
        public async Task SaveStatisticsToFileAsync(string outputPath, bool includeFileTypes = false, CancellationToken cancellationToken = default)
        {
            var root = await GetDetailedStatisticsAsync(cancellationToken);
            var sb = new StringBuilder();

            sb.AppendLine($"百度网盘统计报告");
            sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"统计路径：{_config.PanRootPath}");
            sb.AppendLine($"总文件数：{root.TotalFileCount:N0}");
            sb.AppendLine($"总大小：{FormatSize(root.TotalSizeRecursive)}");
            sb.AppendLine(new string('-', 60));

            AppendNodeText(sb, root, "", true, includeFileTypes);

            EnsureDirectoryExists(outputPath);
            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            LogInfo($"统计结果已保存：{outputPath}");
        }

        private void AppendNodeText(StringBuilder sb, FolderStatisticsNode node, string indent, bool isLast, bool includeFileTypes)
        {
            var prefix = indent + (isLast ? "└─" : "├─");
            sb.AppendLine($"{prefix} {System.IO.Path.GetFileName(node.Path)}  |  文件：{node.FileCount:N0}  |  大小：{FormatSize(node.TotalSize)}");

            if (includeFileTypes && node.FileTypes.Any())
            {
                var extIndent = indent + (isLast ? "   " : "│  ") + "   ";
                var sorted = node.FileTypes.OrderByDescending(x => x.Value.TotalSize).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    var lastExt = i == sorted.Count - 1;
                    var extPrefix = extIndent + (lastExt ? "└─" : "├─");
                    sb.AppendLine($"{extPrefix} {sorted[i].Key}: {sorted[i].Value.Count:N0} 个, {FormatSize(sorted[i].Value.TotalSize)}");
                }
            }

            for (int i = 0; i < node.SubFolders.Count; i++)
            {
                var lastSub = i == node.SubFolders.Count - 1;
                AppendNodeText(sb, node.SubFolders[i], indent + (isLast ? "   " : "│  "), lastSub, includeFileTypes);
            }
        }

        #endregion

        #region CSV导出

        /// <summary>
        /// 导出所有文件信息到CSV
        /// </summary>
        public async Task ExportFilesToCsvAsync(string outputPath, CancellationToken cancellationToken = default)
        {
            LogInfo($"开始导出CSV：{_config.PanRootPath} → {outputPath}");
            EnsureDirectoryExists(outputPath);

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            await writer.WriteLineAsync("\"FsId\",\"Path\",\"ServerFileName\",\"Size\",\"ServerMtime\",\"ServerCtime\",\"LocalMtime\",\"LocalCtime\",\"IsDir\",\"Category\",\"Md5\",\"DirEmpty\",\"Thumbs\"");

            var dirQueue = new Queue<string>();
            dirQueue.Enqueue(_config.PanRootPath);

            while (dirQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                var currentDir = dirQueue.Dequeue();
                var entries = await GetDirectoryEntriesWithCacheAsync(currentDir, cancellationToken);
                if (entries == null) continue;

                foreach (var entry in entries)
                {
                    if (entry.IsDir == 1)
                    {
                        dirQueue.Enqueue(entry.Path);
                    }
                    else
                    {
                        await writer.WriteLineAsync(FormatFileToCsv(entry));
                    }
                }
            }

            LogInfo($"CSV导出完成：{outputPath}");
        }

        private string FormatFileToCsv(BaseFileInfo file)
        {
            string Escape(string value) => value == null ? "" : $"\"{value.Replace("\"", "\"\"")}\"";

            var thumbsJson = file.Thumbs != null ? JsonConvert.SerializeObject(file.Thumbs) : "";

            return $"{Escape(file.FsId.ToString())}," +
                   $"{Escape(file.Path)}," +
                   $"{Escape(file.ServerFileName)}," +
                   $"{Escape(file.Size.ToString())}," +
                   $"{Escape(file.ServerMtime.ToString())}," +
                   $"{Escape(file.ServerCtime.ToString())}," +
                   $"{Escape(file.LocalMtime.ToString())}," +
                   $"{Escape(file.LocalCtime.ToString())}," +
                   $"{Escape(file.IsDir.ToString())}," +
                   $"{Escape(file.Category.ToString())}," +
                   $"{Escape(file.Md5 ?? "")}," +
                   $"{Escape(file.DirEmpty.ToString())}," +
                   $"{Escape(thumbsJson)}";
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectoryExists(string filePath)
        {
            var dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// 组合网盘路径
        /// </summary>
        private static string CombinePath(string parent, string child)
        {
            if (string.IsNullOrEmpty(parent) || parent == "/")
                return $"/{child.TrimStart('/')}";
            return $"{parent.TrimEnd('/')}/{child.TrimStart('/')}";
        }

        /// <summary>
        /// 获取父目录
        /// </summary>
        private static string GetParentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/") return "/";
            var lastSlash = path.LastIndexOf('/');
            return lastSlash <= 0 ? "/" : path[..lastSlash];
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 日志输出
        /// </summary>
        private void LogInfo(string message)
        {
            if (_config.EnableProgressLogging)
                Console.WriteLine($"[INFO] {message}");
        }

        private void LogDebug(string message)
        {
            if (_config.EnableProgressLogging)
                Console.WriteLine($"[DEBUG] {message}");
        }

        private void LogError(string message)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
        }

        #endregion
    }


}