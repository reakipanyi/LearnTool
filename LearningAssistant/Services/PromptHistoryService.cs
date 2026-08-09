using System.Text;

namespace LearningAssistant.Services
{
    /// <summary>
    /// 对话记录结构
    /// </summary>
    public class ConversationRecord
    {
        public DateTime Timestamp { get; set; }
        public string AIProvider { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string? Context { get; set; }
    }

    /// <summary>
    /// AI面板历史记录管理服务
    /// </summary>
    public class PromptHistoryService
    {
        private readonly List<string> _promptHistory = new();
        private readonly List<ConversationRecord> _conversationHistory = new();

        public const int MaxHistoryCount = 20;
        public const int MaxConversationCount = 100;

        /// <summary>
        /// 提示词历史列表
        /// </summary>
        public IReadOnlyList<string> PromptHistory => _promptHistory;

        /// <summary>
        /// 对话历史列表
        /// </summary>
        public IReadOnlyList<ConversationRecord> ConversationHistory => _conversationHistory;

        /// <summary>
        /// 添加提示词到历史记录
        /// </summary>
        public void AddToHistory(string prompt, bool isFavorite = false)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;

            // 移除旧条目（同时处理带收藏前缀和不带前缀的情况）
            _promptHistory.RemoveAll(item => item == prompt || item == $"⭐ {prompt}");
            string itemText = isFavorite ? $"⭐ {prompt}" : prompt;
            _promptHistory.Insert(0, itemText);

            if (_promptHistory.Count > MaxHistoryCount)
                _promptHistory.RemoveAt(_promptHistory.Count - 1);
        }

        /// <summary>
        /// 保存对话记录
        /// </summary>
        public void SaveConversationRecord(string prompt, string aiProvider, string? context = null)
        {
            var record = new ConversationRecord
            {
                Timestamp = DateTime.Now,
                AIProvider = aiProvider,
                Prompt = prompt,
                Context = context
            };

            _conversationHistory.Add(record);
            if (_conversationHistory.Count > MaxConversationCount)
                _conversationHistory.RemoveAt(0);
        }

        /// <summary>
        /// 清空对话历史
        /// </summary>
        public void ClearConversationHistory()
        {
            _conversationHistory.Clear();
        }

        /// <summary>
        /// 清空提示词历史
        /// </summary>
        public void ClearPromptHistory()
        {
            _promptHistory.Clear();
        }

        /// <summary>
        /// 导出对话历史到文件
        /// </summary>
        public void ExportConversationHistory(string filePath)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.WriteLine("AI对话历史记录");
            writer.WriteLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"总记录数: {_conversationHistory.Count}");
            writer.WriteLine(new string('-', 50));

            foreach (var record in _conversationHistory)
            {
                writer.WriteLine($"时间: {record.Timestamp:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"AI: {record.AIProvider}");
                writer.WriteLine($"提示词: {record.Prompt}");
                if (!string.IsNullOrEmpty(record.Context))
                    writer.WriteLine($"上下文: {record.Context}");
                writer.WriteLine(new string('-', 30));
            }
        }

        /// <summary>
        /// 获取历史记录下拉框显示项
        /// </summary>
        public List<string> GetHistoryComboBoxItems(int maxItems = 10)
        {
            var items = new List<string> { "📜 历史记录" };
            items.AddRange(_promptHistory.Take(maxItems));
            return items;
        }

        /// <summary>
        /// 从历史项中提取实际提示词（移除收藏标记）
        /// </summary>
        public static string ExtractPromptFromHistoryItem(string item)
        {
            if (string.IsNullOrEmpty(item)) return "";
            return item.StartsWith("⭐ ") ? item.Substring(2) : item;
        }
    }
}