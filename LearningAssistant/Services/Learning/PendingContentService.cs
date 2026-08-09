using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 待添加内容服务，用于在不同窗口间传递学习内容
    /// </summary>
    public interface IPendingContentService
    {
        /// <summary>
        /// 添加内容到待处理队列
        /// </summary>
        void Add(string content, string language, string category = "中文综合");

        /// <summary>
        /// 获取所有待添加内容
        /// </summary>
        List<PendingContentItem> GetAll();

        /// <summary>
        /// 清除所有待添加内容
        /// </summary>
        void Clear();

        /// <summary>
        /// 保存到文件
        /// </summary>
        void SaveToFile();

        /// <summary>
        /// 从文件加载
        /// </summary>
        void LoadFromFile();
    }

    public class PendingContentItem
    {
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class PendingContentService : IPendingContentService
    {
        private readonly ILogger<PendingContentService>? _logger;
        private readonly ConcurrentBag<PendingContentItem> _pendingItems = new();
        private readonly string _filePath;

        public PendingContentService(ILogger<PendingContentService>? logger = null)
        {
            _logger = logger;
            _filePath = AppPaths.PendingContentPath;
            Directory.CreateDirectory(AppPaths.ConfigDir);
            LoadFromFile();
        }

        public void Add(string content, string language, string category = "中文综合")
        {
            var item = new PendingContentItem
            {
                Content = content,
                Language = language,
                Category = category,
                AddedAt = DateTime.Now
            };
            _pendingItems.Add(item);
            SaveToFile();
            _logger?.LogInformation("Added pending content: {Category} - {Language}", category, language);
        }

        public List<PendingContentItem> GetAll()
        {
            return _pendingItems.ToList();
        }

        public void Clear()
        {
            _pendingItems.Clear();
            SaveToFile();
        }

        public void SaveToFile()
        {
            try
            {
                var items = _pendingItems.ToList();
                // 使用 JsonHelper.SaveToFile 原子写入（temp+replace），避免写入中途崩溃导致文件损坏、
                // 下次加载反序列化失败而丢失全部待处理内容；同时统一到项目 System.Text.Json 序列化栈。
                JsonHelper.SaveToFile(_filePath, items);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save pending content");
            }
        }

        public void LoadFromFile()
        {
            var items = JsonHelper.LoadFromFile<List<PendingContentItem>>(_filePath);
            if (items == null || items.Count == 0)
                return;

            // 加载即替换：先清空再追加，避免重复调用导致条目复制。
            _pendingItems.Clear();
            foreach (var item in items)
            {
                _pendingItems.Add(item);
            }
        }
    }
}
