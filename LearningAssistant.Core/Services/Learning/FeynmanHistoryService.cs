using LearningAssistant.Models.Learning;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LearningAssistant.Common;

using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 费曼学习法历史记录服务
    /// 提供历史记录的保存、查询和对比功能
    /// </summary>
    public class FeynmanHistoryService
    {
        private readonly string _storagePath;
        private readonly ILogger<FeynmanHistoryService>? _logger;
        private readonly object _lock = new();
        private FeynmanHistoryStore _store = new();
        private readonly IAppPaths _appPaths;

        public FeynmanHistoryService(ILogger<FeynmanHistoryService>? logger = null, IAppPaths appPaths = null)
        {
            _logger = logger;
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _storagePath = Path.Combine(_appPaths.CurrentUserDir, "feynman_history.json");
            _appPaths.EnsureDirectoryExists(Path.GetDirectoryName(_storagePath));
            Load();
        }

        /// <summary>
        /// 保存一条费曼学习记录
        /// </summary>
        public void SaveRecord(FeynmanHistoryRecord record)
        {
            lock (_lock)
            {
                var existing = _store.Records.FirstOrDefault(r => r.Id == record.Id);
                if (existing != null)
                {
                    _store.Records.Remove(existing);
                }
                _store.Records.Add(record);
                _store.LastUpdated = DateTime.Now;
                Persist();
            }
        }

        /// <summary>
        /// 获取指定内容的所有历史记录，按时间倒序
        /// </summary>
        public List<FeynmanHistoryRecord> GetRecordsByContent(string contentId, int maxCount = 10)
        {
            lock (_lock)
            {
                return _store.Records
                    .Where(r => r.ContentId.Equals(contentId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.CompletedAt)
                    .Take(maxCount)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取指定内容的最新一条记录
        /// </summary>
        public FeynmanHistoryRecord? GetLatestRecord(string contentId)
        {
            lock (_lock)
            {
                return _store.Records
                    .Where(r => r.ContentId.Equals(contentId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.CompletedAt)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// 获取所有有历史记录的内容ID列表
        /// </summary>
        public List<string> GetAllContentIds()
        {
            lock (_lock)
            {
                return _store.Records
                    .Select(r => r.ContentId)
                    .Distinct()
                    .ToList();
            }
        }

        /// <summary>
        /// 删除指定记录
        /// </summary>
        public void DeleteRecord(string recordId)
        {
            lock (_lock)
            {
                var record = _store.Records.FirstOrDefault(r => r.Id == recordId);
                if (record != null)
                {
                    _store.Records.Remove(record);
                    Persist();
                }
            }
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetTotalCount()
        {
            lock (_lock)
            {
                return _store.Records.Count;
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    var store = JsonSerializer.Deserialize<FeynmanHistoryStore>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (store != null)
                    {
                        _store = store;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载费曼学习历史记录失败");
            }
        }

        private void Persist()
        {
            try
            {
                var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(_storagePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存费曼学习历史记录失败");
            }
        }
    }
}
