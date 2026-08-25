using Newtonsoft.Json;

namespace LearningAssistant.Services.Learning
{
    public interface IProgressiveHintStateService
    {
        HintProgress? GetProgress(string contentId, string userId);
        void SaveProgress(string contentId, string userId, HintProgress progress);
        void ClearProgress(string contentId, string userId);
        void ClearAllProgress(string userId);
    }

    public class HintProgress
    {
        public string ContentId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int CurrentHintLevel { get; set; }
        public List<int> ViewedHints { get; set; } = new();
        public string UserGuess { get; set; } = string.Empty;
        public DateTime LastAccessed { get; set; }
    }

    public class ProgressiveHintStateService : IProgressiveHintStateService
    {
        private readonly string _dataDir;
        private readonly Dictionary<string, HintProgress> _cache = new();
        private readonly object _lock = new();
        private string StateFilePath => Path.Combine(_dataDir, "progressive_hint_state.json");

        public ProgressiveHintStateService()
        {
            _dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LearningAssistant",
                "ProgressiveHints");

            Directory.CreateDirectory(_dataDir);
            LoadFromFile();
        }

        public HintProgress? GetProgress(string contentId, string userId)
        {
            lock (_lock)
            {
                string key = BuildKey(contentId, userId);
                if (_cache.TryGetValue(key, out var progress))
                {
                    if ((DateTime.Now - progress.LastAccessed).TotalDays > 7)
                    {
                        _cache.Remove(key);
                        return null;
                    }
                    return progress;
                }
                return null;
            }
        }

        public void SaveProgress(string contentId, string userId, HintProgress progress)
        {
            lock (_lock)
            {
                string key = BuildKey(contentId, userId);
                progress.ContentId = contentId;
                progress.UserId = userId;
                progress.LastAccessed = DateTime.Now;
                _cache[key] = progress;
                SaveToFile();
            }
        }

        public void ClearProgress(string contentId, string userId)
        {
            lock (_lock)
            {
                string key = BuildKey(contentId, userId);
                _cache.Remove(key);
                SaveToFile();
            }
        }

        public void ClearAllProgress(string userId)
        {
            lock (_lock)
            {
                var keysToRemove = _cache.Keys.Where(k => k.EndsWith($"_{userId}")).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                SaveToFile();
            }
        }

        private static string BuildKey(string contentId, string userId)
        {
            return $"{GetHash(contentId)}_{userId}";
        }

        private static string GetHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "empty";
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).Substring(0, 16);
        }

        private void LoadFromFile()
        {
            try
            {
                if (File.Exists(StateFilePath))
                {
                    string json = File.ReadAllText(StateFilePath);
                    var list = JsonConvert.DeserializeObject<List<HintProgress>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if ((DateTime.Now - item.LastAccessed).TotalDays <= 7)
                            {
                                string key = BuildKey(item.ContentId, item.UserId);
                                _cache[key] = item;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveToFile()
        {
            try
            {
                var list = _cache.Values.ToList();
                string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(StateFilePath, json);
            }
            catch
            {
            }
        }
    }
}
