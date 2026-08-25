using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.Recovery;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace LearningAssistant.Services.Recovery
{
    /// <summary>
    /// 崩溃恢复与自动保存服务实现
    /// </summary>
    public class CrashRecoveryService : ICrashRecoveryService, IDisposable
    {
        private readonly ILogger<CrashRecoveryService>? _logger;
        private AutoSaveConfig _config;
        private Timer? _autoSaveTimer;
        private readonly List<IAutoSaveProvider> _providers = new();
        private readonly object _lock = new();
        private bool _disposed;
        private readonly IAppPaths _appPaths;

        public bool AutoSaveEnabled
        {
            get => _config.AutoSaveEnabled;
            set
            {
                _config.AutoSaveEnabled = value;
                if (value) StartAutoSave();
                else StopAutoSave();
                SaveConfig();
                AutoSaveStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int AutoSaveInterval
        {
            get => _config.AutoSaveIntervalSeconds;
            set
            {
                _config.AutoSaveIntervalSeconds = value;
                if (_autoSaveTimer != null && _autoSaveTimer.Enabled)
                {
                    _autoSaveTimer.Interval = TimeSpan.FromSeconds(value).TotalMilliseconds;
                }
                SaveConfig();
                AutoSaveStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DateTime? LastAutoSaveTime { get; private set; }

        public event EventHandler? AutoSaveStateChanged;

        public CrashRecoveryService(ILogger<CrashRecoveryService>? logger = null, IAppPaths appPaths = null!)
        {
            _logger = logger;
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _config = LoadConfig();
            EnsureAutoSaveDirectory();
        }

        /// <inheritdoc/>
        public void StartAutoSave()
        {
            lock (_lock)
            {
                if (_autoSaveTimer != null)
                {
                    _autoSaveTimer.Stop();
                    _autoSaveTimer.Dispose();
                }

                if (!_config.AutoSaveEnabled || _config.AutoSaveIntervalSeconds <= 0)
                    return;

                _autoSaveTimer = new Timer(TimeSpan.FromSeconds(_config.AutoSaveIntervalSeconds).TotalMilliseconds);
                _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
                _autoSaveTimer.AutoReset = true;
                _autoSaveTimer.Start();

                _logger?.LogInformation("自动保存已启动，间隔 {Interval} 秒", _config.AutoSaveIntervalSeconds);
            }
        }

        /// <inheritdoc/>
        public void StopAutoSave()
        {
            lock (_lock)
            {
                if (_autoSaveTimer != null)
                {
                    _autoSaveTimer.Stop();
                    _autoSaveTimer.Dispose();
                    _autoSaveTimer = null;
                }
            }
        }

        /// <inheritdoc/>
        public void SaveNow()
        {
            try
            {
                if (_providers.Count == 0)
                    return;

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var snapshotId = $"snapshot_{timestamp}";

                foreach (var provider in _providers)
                {
                    try
                    {
                        var fileName = $"{provider.DataType}_{timestamp}.json";
                        var filePath = Path.Combine(GetAutoSaveDirectory(), fileName);

                        provider.SaveTo(filePath);

                        var snapshot = new AutoSaveSnapshot
                        {
                            SnapshotId = snapshotId,
                            CreatedAt = DateTime.Now,
                            FilePath = filePath,
                            FileSize = new FileInfo(filePath).Length,
                            Description = provider.GetDescription(),
                            DataType = provider.DataType
                        };

                        SaveSnapshotInfo(snapshot);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "自动保存失败: {DataType}", provider.DataType);
                    }
                }

                LastAutoSaveTime = DateTime.Now;
                CleanOldSnapshots();

                _logger?.LogDebug("自动保存完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "自动保存失败");
            }
        }

        /// <inheritdoc/>
        public void RegisterProvider(IAutoSaveProvider provider)
        {
            lock (_providers)
            {
                var existing = _providers.FirstOrDefault(p => p.DataType == provider.DataType);
                if (existing != null)
                {
                    _providers.Remove(existing);
                }
                _providers.Add(provider);
                _logger?.LogInformation("已注册自动保存提供者: {DataType}", provider.DataType);
            }
        }

        /// <inheritdoc/>
        public void UnregisterProvider(string dataType)
        {
            lock (_providers)
            {
                var provider = _providers.FirstOrDefault(p => p.DataType == dataType);
                if (provider != null)
                {
                    _providers.Remove(provider);
                    _logger?.LogInformation("已注销自动保存提供者: {DataType}", dataType);
                }
            }
        }

        /// <inheritdoc/>
        public bool CheckLastExitWasCrash()
        {
            try
            {
                var markerFile = Path.Combine(GetAutoSaveDirectory(), "app_running.marker");
                var lastExitFile = Path.Combine(GetAutoSaveDirectory(), "last_exit.json");

                if (File.Exists(markerFile))
                {
                    _logger?.LogWarning("检测到上次可能异常退出");
                    return true;
                }

                if (File.Exists(lastExitFile))
                {
                    var json = File.ReadAllText(lastExitFile);
                    var exitInfo = JsonSerializer.Deserialize<ExitInfo>(json);
                    return exitInfo?.WasCrashed ?? false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "检查上次退出状态失败");
                return false;
            }
        }

        /// <inheritdoc/>
        public List<AutoSaveSnapshot> GetRecoverableSnapshots()
        {
            try
            {
                var snapshots = new List<AutoSaveSnapshot>();
                var infoDir = Path.Combine(GetAutoSaveDirectory(), "snapshots");

                if (!Directory.Exists(infoDir))
                    return snapshots;

                var infoFiles = Directory.GetFiles(infoDir, "*.json")
                    .OrderByDescending(f => f)
                    .ToList();

                foreach (var infoFile in infoFiles)
                {
                    try
                        {
                            var json = File.ReadAllText(infoFile);
                            var snapshot = JsonSerializer.Deserialize<AutoSaveSnapshot>(json);
                            if (snapshot != null && File.Exists(snapshot.FilePath))
                            {
                                snapshots.Add(snapshot);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "无法读取快照信息文件: {File}", infoFile);
                        }
                }

                return snapshots;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取可恢复快照失败");
                return new List<AutoSaveSnapshot>();
            }
        }

        /// <inheritdoc/>
        public RecoveryResult RestoreFromSnapshot(string snapshotId)
        {
            var result = new RecoveryResult();

            try
            {
                var snapshots = GetRecoverableSnapshots();
                var targetSnapshots = snapshots.Where(s => s.SnapshotId == snapshotId).ToList();

                if (targetSnapshots.Count == 0)
                {
                    result.ErrorMessage = "未找到指定的快照";
                    return result;
                }

                foreach (var snapshot in targetSnapshots)
                {
                    var provider = _providers.FirstOrDefault(p => p.DataType == snapshot.DataType);
                    if (provider != null)
                    {
                        if (provider.RestoreFrom(snapshot.FilePath))
                        {
                            result.RecoveredCount++;
                            result.RecoveredFiles.Add(snapshot.FilePath);
                        }
                    }
                }

                result.Success = result.RecoveredCount > 0;
                result.HasRecoverableData = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger?.LogError(ex, "从快照恢复失败");
            }

            return result;
        }

        /// <inheritdoc/>
        public RecoveryResult RestoreLatest()
        {
            try
            {
                var snapshots = GetRecoverableSnapshots();
                if (snapshots.Count == 0)
                {
                    return new RecoveryResult
                    {
                        HasRecoverableData = false,
                        ErrorMessage = "没有可恢复的快照"
                    };
                }

                var latestSnapshot = snapshots.OrderByDescending(s => s.CreatedAt).First();
                return RestoreFromSnapshot(latestSnapshot.SnapshotId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "恢复最近快照失败");
                return new RecoveryResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <inheritdoc/>
        public AutoSaveConfig GetConfig()
        {
            return _config;
        }

        /// <inheritdoc/>
        public void UpdateConfig(AutoSaveConfig config)
        {
            _config = config;
            SaveConfig();

            if (config.AutoSaveEnabled)
            {
                StartAutoSave();
            }
            else
            {
                StopAutoSave();
            }
        }

        /// <inheritdoc/>
        public void MarkNormalExit()
        {
            try
            {
                SaveNow();

                var markerFile = Path.Combine(GetAutoSaveDirectory(), "app_running.marker");
                if (File.Exists(markerFile))
                {
                    File.Delete(markerFile);
                }

                var exitInfo = new ExitInfo
                {
                    ExitTime = DateTime.Now,
                    WasCrashed = false
                };
                var lastExitFile = Path.Combine(GetAutoSaveDirectory(), "last_exit.json");
                File.WriteAllText(lastExitFile, JsonSerializer.Serialize(exitInfo));

                _logger?.LogInformation("已标记正常退出");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "标记正常退出失败");
            }
        }

        /// <inheritdoc/>
        public void MarkAppStarted()
        {
            try
            {
                EnsureAutoSaveDirectory();

                var markerFile = Path.Combine(GetAutoSaveDirectory(), "app_running.marker");
                File.WriteAllText(markerFile, DateTime.Now.ToString("o"));

                if (_config.AutoSaveEnabled)
                {
                    StartAutoSave();
                }

                _logger?.LogInformation("应用启动已标记");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "标记应用启动失败");
            }
        }

        /// <inheritdoc/>
        public void CleanOldSnapshots(int? maxFiles = null)
        {
            try
            {
                var limit = maxFiles ?? _config.MaxAutoSaveFiles;
                var infoDir = Path.Combine(GetAutoSaveDirectory(), "snapshots");

                if (!Directory.Exists(infoDir))
                    return;

                var allSnapshots = GetRecoverableSnapshots();
                if (allSnapshots.Count <= limit)
                    return;

                var snapshotsByTime = allSnapshots.OrderBy(s => s.CreatedAt).ToList();
                var toDelete = snapshotsByTime.Take(snapshotsByTime.Count - limit).ToList();

                foreach (var snapshot in toDelete)
                {
                    try
                    {
                        if (File.Exists(snapshot.FilePath))
                        {
                            File.Delete(snapshot.FilePath);
                        }

                        var infoFile = Path.Combine(infoDir, $"{snapshot.SnapshotId}_{snapshot.DataType}.json");
                        if (File.Exists(infoFile))
                        {
                            File.Delete(infoFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "清理旧快照文件失败: {File}", snapshot.FilePath);
                    }
                }

                _logger?.LogDebug("已清理 {Count} 个旧快照", toDelete.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清理旧快照失败");
            }
        }

        #region 私有方法

        private void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                SaveNow();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "定时自动保存失败");
            }
        }

        private string GetAutoSaveDirectory()
        {
            if (!string.IsNullOrEmpty(_config.AutoSaveDirectory) && Directory.Exists(_config.AutoSaveDirectory))
            {
                return _config.AutoSaveDirectory;
            }

            return Path.Combine(_appPaths.UserDataDir, "autosave");
        }

        private void EnsureAutoSaveDirectory()
        {
            try
            {
                var dir = GetAutoSaveDirectory();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var snapshotsDir = Path.Combine(dir, "snapshots");
                if (!Directory.Exists(snapshotsDir))
                {
                    Directory.CreateDirectory(snapshotsDir);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建自动保存目录失败");
            }
        }

        private void SaveSnapshotInfo(AutoSaveSnapshot snapshot)
        {
            try
            {
                var infoDir = Path.Combine(GetAutoSaveDirectory(), "snapshots");
                var infoFile = Path.Combine(infoDir, $"{snapshot.SnapshotId}_{snapshot.DataType}.json");
                File.WriteAllText(infoFile, JsonSerializer.Serialize(snapshot));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存快照信息失败");
            }
        }

        private AutoSaveConfig LoadConfig()
        {
            try
            {
                var configPath = Path.Combine(_appPaths.ConfigDir, "AutoSaveSettings.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<AutoSaveConfig>(json);
                    if (config != null)
                        return config;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载自动保存配置失败，使用默认配置");
            }

            return GetDefaultConfig();
        }

        private void SaveConfig()
        {
            try
            {
                var configPath = Path.Combine(_appPaths.ConfigDir, "AutoSaveSettings.json");
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(configPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存自动保存配置失败");
            }
        }

        private AutoSaveConfig GetDefaultConfig()
        {
            return new AutoSaveConfig
            {
                AutoSaveEnabled = true,
                AutoSaveIntervalSeconds = 60,
                MaxAutoSaveFiles = 10,
                CrashRecoveryEnabled = true
            };
        }

        private class ExitInfo
        {
            public DateTime ExitTime { get; set; }
            public bool WasCrashed { get; set; }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopAutoSave();
            MarkNormalExit();
        }

        #endregion
    }
}
