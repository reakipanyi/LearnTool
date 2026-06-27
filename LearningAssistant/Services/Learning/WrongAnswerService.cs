using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 错题本服务实现
    /// 提供错题记录、分类、复习、统计等功能
    /// </summary>
    public class WrongAnswerService : IWrongAnswerService, IDisposable
    {
        private readonly IDataPersistenceService _persistenceService;
        private readonly ILogger<WrongAnswerService> _logger;
        private readonly IEventBus? _eventBus;
        private readonly string _wrongAnswersDir;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public WrongAnswerService(
            IDataPersistenceService persistenceService,
            ILogger<WrongAnswerService> logger,
            IEventBus? eventBus = null)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus;
            _wrongAnswersDir = Path.Combine(AppPaths.UsersDir, "wrong_answers");
            EnsureDirectoryExists();

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Subscribe<ItemWrongEvent>(OnItemWrong);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<ItemWrongEvent>(OnItemWrong);
        }

        private void OnItemWrong(ItemWrongEvent evt)
        {
            try
            {
                var item = new WrongAnswerItem
                {
                    Question = evt.ItemContent,
                    CorrectAnswer = evt.CorrectAnswer,
                    UserAnswer = evt.UserAnswer,
                    Subject = evt.SubCategory,
                    Category = evt.SubCategory
                };

                AddWrongAnswer(evt.UserId, item);

                _logger.LogInformation("自动记录错题: {UserId}, {Question}", 
                    evt.UserId, 
                    evt.ItemContent.Length > 30 ? evt.ItemContent.Substring(0, 30) + "..." : evt.ItemContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理学习项答错事件失败: {ItemContent}", evt.ItemContent);
            }
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_wrongAnswersDir))
            {
                Directory.CreateDirectory(_wrongAnswersDir);
            }
        }

        private string GetUserWrongAnswersPath(string userId)
        {
            return Path.Combine(_wrongAnswersDir, $"{userId}_wrong_answers.json");
        }

        #region 基础 CRUD（兼容旧接口）

        public void AddWrongAnswer(string userId, WrongAnswerItem item)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            lock (_lock)
            {
                try
                {
                    var items = LoadWrongAnswers(userId);

                    var existing = items.FirstOrDefault(i =>
                        i.Question == item.Question && i.Subject == item.Subject);

                    if (existing != null)
                    {
                        existing.WrongCount++;
                        existing.Mastery = MasteryLevel.NotMastered;
                        existing.LastWrongAt = DateTime.Now;
                        existing.LastReviewAt = DateTime.Now;
                        _logger.LogInformation("用户 {UserId} 错题已存在，错误次数+1: {Question}", userId, item.Question);
                    }
                    else
                    {
                        item.Id = Guid.NewGuid().ToString();
                        item.UserId = userId;
                        item.FirstWrongAt = DateTime.Now;
                        item.LastWrongAt = DateTime.Now;
                        item.AddedAt = DateTime.Now;
                        item.IsActive = true;
                        items.Add(item);
                        _logger.LogInformation("用户 {UserId} 添加错题: {Question}", userId, item.Question);
                    }

                    SaveWrongAnswers(userId, items);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "添加错题失败: {UserId}", userId);
                    throw;
                }
            }
        }

        public void RemoveWrongAnswer(string userId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("错题ID不能为空", nameof(itemId));

            lock (_lock)
            {
                try
                {
                    var items = LoadWrongAnswers(userId);
                    var item = items.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        items.Remove(item);
                        SaveWrongAnswers(userId, items);
                        _logger.LogInformation("用户 {UserId} 删除错题: {Question}", userId, item.Question);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除错题失败: {UserId} - {ItemId}", userId, itemId);
                    throw;
                }
            }
        }

        public List<WrongAnswerItem> GetWrongAnswers(string userId, string subject = "", string category = "")
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<WrongAnswerItem>();

            try
            {
                var items = LoadWrongAnswers(userId).Where(i => i.IsActive).ToList();

                if (!string.IsNullOrWhiteSpace(subject))
                {
                    items = items.Where(i => i.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    items = items.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return items.OrderByDescending(i => i.AddedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取错题列表失败: {UserId}", userId);
                return new List<WrongAnswerItem>();
            }
        }

        public List<WrongAnswerItem> GetWrongAnswersForReview(string userId, int count = 10)
        {
            if (string.IsNullOrWhiteSpace(userId) || count <= 0)
                return new List<WrongAnswerItem>();

            try
            {
                var items = LoadWrongAnswers(userId)
                    .Where(i => i.IsActive && i.Mastery != MasteryLevel.Mastered)
                    .OrderByDescending(i => i.WrongCount)
                    .ThenBy(i => i.LastReviewAt ?? DateTime.MinValue)
                    .Take(count)
                    .ToList();

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取复习错题失败: {UserId}", userId);
                return new List<WrongAnswerItem>();
            }
        }

        public void MarkAsReviewed(string userId, string itemId, bool remembered)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            try
            {
                var items = LoadWrongAnswers(userId);
                var item = items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    item.LastReviewAt = DateTime.Now;
                    item.ReviewCount++;

                    if (remembered)
                    {
                        item.Difficulty = Math.Max(0, item.Difficulty - 0.1);
                        if (item.Difficulty <= 0.2 && item.ReviewCount >= 3)
                        {
                            item.Mastery = MasteryLevel.Mastered;
                        }
                        else if (item.Mastery == MasteryLevel.NotMastered && item.ReviewCount >= 2)
                        {
                            item.Mastery = MasteryLevel.Fuzzy;
                        }
                    }
                    else
                    {
                        item.Difficulty = Math.Min(1, item.Difficulty + 0.2);
                        item.WrongCount++;
                        item.LastWrongAt = DateTime.Now;
                        item.Mastery = MasteryLevel.NotMastered;
                    }

                    SaveWrongAnswers(userId, items);
                    _logger.LogInformation("用户 {UserId} 复习错题: {Question}, 记住: {Remembered}", userId, item.Question, remembered);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记复习状态失败: {UserId} - {ItemId}", userId, itemId);
            }
        }

        public void MarkAsMastered(string userId, string itemId)
        {
            UpdateMastery(userId, itemId, MasteryLevel.Mastered);
        }

        public int GetWrongAnswerCount(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            try
            {
                return LoadWrongAnswers(userId).Count(i => i.IsActive && i.Mastery != MasteryLevel.Mastered);
            }
            catch
            {
                return 0;
            }
        }

        public int GetMasteredCount(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return 0;

            try
            {
                return LoadWrongAnswers(userId).Count(i => i.IsActive && i.Mastery == MasteryLevel.Mastered);
            }
            catch
            {
                return 0;
            }
        }

        public void ExportWrongAnswers(string userId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            try
            {
                var items = GetWrongAnswers(userId);

                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("错题本导出");
                writer.WriteLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"错题总数: {items.Count}");
                writer.WriteLine(new string('=', 50));
                writer.WriteLine();

                int index = 1;
                foreach (var item in items)
                {
                    writer.WriteLine($"【第 {index} 题】");
                    writer.WriteLine($"科目: {item.Subject}");
                    writer.WriteLine($"分类: {item.Category}");
                    writer.WriteLine($"问题: {item.Question}");
                    writer.WriteLine($"正确答案: {item.CorrectAnswer}");
                    writer.WriteLine($"你的答案: {item.UserAnswer}");
                    writer.WriteLine($"解析: {item.Explanation}");
                    writer.WriteLine($"错误次数: {item.WrongCount}");
                    writer.WriteLine($"复习次数: {item.ReviewCount}");
                    writer.WriteLine($"掌握程度: {item.MasteryText}");
                    writer.WriteLine($"添加时间: {item.AddedAt:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();
                    index++;
                }

                _logger.LogInformation("用户 {UserId} 导出错题本到: {FilePath}", userId, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出错题本失败: {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region 筛选与搜索

        public List<WrongAnswerItem> GetWrongAnswers(string userId, WrongAnswerFilter filter)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<WrongAnswerItem>();

            try
            {
                var items = LoadWrongAnswers(userId).Where(i => i.IsActive).AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.Subject))
                {
                    items = items.Where(i => i.Subject.Equals(filter.Subject, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filter.Category))
                {
                    items = items.Where(i => i.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));
                }

                if (filter.Mastery.HasValue)
                {
                    items = items.Where(i => i.Mastery == filter.Mastery.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                {
                    var keyword = filter.Keyword.ToLower();
                    items = items.Where(i =>
                        (i.Question ?? string.Empty).ToLower().Contains(keyword) ||
                        (i.CorrectAnswer ?? string.Empty).ToLower().Contains(keyword) ||
                        (i.Explanation ?? string.Empty).ToLower().Contains(keyword));
                }

                if (filter.MinWrongCount.HasValue)
                {
                    items = items.Where(i => i.WrongCount >= filter.MinWrongCount.Value);
                }

                if (filter.FromDate.HasValue)
                {
                    items = items.Where(i => i.AddedAt >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    items = items.Where(i => i.AddedAt <= filter.ToDate.Value);
                }

                if (filter.Tags != null && filter.Tags.Count > 0)
                {
                    items = items.Where(i => i.TagsList.Any(t => filter.Tags.Contains(t)));
                }

                IQueryable<WrongAnswerItem> query = items.OrderByDescending(i => i.AddedAt);

                if (filter.Skip.HasValue)
                {
                    query = query.Skip(filter.Skip.Value);
                }

                if (filter.Take.HasValue)
                {
                    query = query.Take(filter.Take.Value);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按筛选条件获取错题失败: {UserId}", userId);
                return new List<WrongAnswerItem>();
            }
        }

        public List<WrongAnswerItem> GetWrongAnswers(string userId, int skip, int take)
        {
            var filter = new WrongAnswerFilter { Skip = skip, Take = take };
            return GetWrongAnswers(userId, filter);
        }

        public List<WrongAnswerItem> SearchWrongAnswers(string userId, string keyword)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(keyword))
                return new List<WrongAnswerItem>();

            var filter = new WrongAnswerFilter { Keyword = keyword };
            return GetWrongAnswers(userId, filter);
        }

        public (List<WrongAnswerItem> items, int total) GetWrongAnswersPaged(
            string userId, WrongAnswerFilter filter, int page = 1, int pageSize = 20)
        {
            var allItems = GetWrongAnswers(userId, filter);
            var total = allItems.Count;
            var items = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return (items, total);
        }

        #endregion

        #region 掌握程度管理

        public void UpdateMastery(string userId, string itemId, MasteryLevel mastery)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId))
                return;

            lock (_lock)
            {
                try
                {
                    var items = LoadWrongAnswers(userId);
                    var item = items.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        item.Mastery = mastery;
                        SaveWrongAnswers(userId, items);
                        _logger.LogInformation("用户 {UserId} 更新错题掌握程度: {Question} - {Mastery}", userId, item.Question, mastery);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新掌握程度失败: {UserId} - {ItemId}", userId, itemId);
                }
            }
        }

        public void BatchUpdateMastery(string userId, List<string> itemIds, MasteryLevel mastery)
        {
            if (string.IsNullOrWhiteSpace(userId) || itemIds == null || itemIds.Count == 0)
                return;

            lock (_lock)
            {
                try
                {
                    var items = LoadWrongAnswers(userId);
                    bool changed = false;

                    foreach (var itemId in itemIds)
                    {
                        var item = items.FirstOrDefault(i => i.Id == itemId);
                        if (item != null && item.Mastery != mastery)
                        {
                            item.Mastery = mastery;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        SaveWrongAnswers(userId, items);
                        _logger.LogInformation("用户 {UserId} 批量更新掌握程度: {Count} 道题 - {Mastery}", userId, itemIds.Count, mastery);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量更新掌握程度失败: {UserId}", userId);
                }
            }
        }

        public void BatchRemove(string userId, List<string> itemIds)
        {
            if (string.IsNullOrWhiteSpace(userId) || itemIds == null || itemIds.Count == 0)
                return;

            lock (_lock)
            {
                try
                {
                    var items = LoadWrongAnswers(userId);
                    var toRemove = items.Where(i => itemIds.Contains(i.Id)).ToList();
                    items.RemoveAll(i => itemIds.Contains(i.Id));
                    SaveWrongAnswers(userId, items);
                    _logger.LogInformation("用户 {UserId} 批量删除错题: {Count} 道", userId, toRemove.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量删除错题失败: {UserId}", userId);
                }
            }
        }

        #endregion

        #region 标签管理

        public Dictionary<string, int> GetAllTags(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new Dictionary<string, int>();

            try
            {
                var items = LoadWrongAnswers(userId).Where(i => i.IsActive);
                var tagDict = new Dictionary<string, int>();

                foreach (var item in items)
                {
                    foreach (var tag in item.TagsList)
                    {
                        if (tagDict.ContainsKey(tag))
                            tagDict[tag]++;
                        else
                            tagDict[tag] = 1;
                    }
                }

                return tagDict.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有标签失败: {UserId}", userId);
                return new Dictionary<string, int>();
            }
        }

        public void AddTag(string userId, string itemId, string tag)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(tag))
                return;

            try
            {
                var items = LoadWrongAnswers(userId);
                var item = items.FirstOrDefault(i => i.Id == itemId);
                if (item != null && !item.TagsList.Contains(tag))
                {
                    item.TagsList.Add(tag);
                    SaveWrongAnswers(userId, items);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加标签失败: {UserId} - {ItemId}", userId, itemId);
            }
        }

        public void RemoveTag(string userId, string itemId, string tag)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(tag))
                return;

            try
            {
                var items = LoadWrongAnswers(userId);
                var item = items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    item.TagsList.Remove(tag);
                    SaveWrongAnswers(userId, items);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除标签失败: {UserId} - {ItemId}", userId, itemId);
            }
        }

        #endregion

        #region 统计信息

        public WrongAnswerStats GetStatistics(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new WrongAnswerStats();

            try
            {
                var items = LoadWrongAnswers(userId).Where(i => i.IsActive).ToList();
                var stats = new WrongAnswerStats
                {
                    TotalCount = items.Count,
                    NotMasteredCount = items.Count(i => i.Mastery == MasteryLevel.NotMastered),
                    FuzzyCount = items.Count(i => i.Mastery == MasteryLevel.Fuzzy),
                    MasteredCount = items.Count(i => i.Mastery == MasteryLevel.Mastered),
                    TotalWrongCount = items.Sum(i => i.WrongCount)
                };

                stats.SubjectStats = items
                    .GroupBy(i => i.Subject)
                    .Select(g => new { Subject = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToDictionary(x => string.IsNullOrWhiteSpace(x.Subject) ? "未分类" : x.Subject, x => x.Count);

                stats.TopWrongItems = items
                    .OrderByDescending(i => i.WrongCount)
                    .Take(10)
                    .ToList();

                stats.TagStats = GetAllTags(userId);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计信息失败: {UserId}", userId);
                return new WrongAnswerStats();
            }
        }

        public List<string> GetSubjects(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<string>();

            try
            {
                return LoadWrongAnswers(userId)
                    .Where(i => i.IsActive && !string.IsNullOrWhiteSpace(i.Subject))
                    .Select(i => i.Subject)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取学科列表失败: {UserId}", userId);
                return new List<string>();
            }
        }

        public List<string> GetCategories(string userId, string subject)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<string>();

            try
            {
                var items = LoadWrongAnswers(userId).Where(i => i.IsActive);

                if (!string.IsNullOrWhiteSpace(subject))
                {
                    items = items.Where(i => i.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase));
                }

                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Category))
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分类列表失败: {UserId}", userId);
                return new List<string>();
            }
        }

        #endregion

        #region 导出功能

        public bool ExportToMarkdown(string userId, string filePath, WrongAnswerFilter? filter = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var items = filter != null
                    ? GetWrongAnswers(userId, filter)
                    : GetWrongAnswers(userId);

                var sb = new StringBuilder();
                sb.AppendLine("# 错题本导出");
                sb.AppendLine();
                sb.AppendLine($"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"> 共 {items.Count} 道错题");
                sb.AppendLine();

                var groupedBySubject = items.GroupBy(i => i.Subject);
                foreach (var group in groupedBySubject)
                {
                    var subjectName = string.IsNullOrWhiteSpace(group.Key) ? "未分类" : group.Key;
                    sb.AppendLine($"## 📚 {subjectName}");
                    sb.AppendLine();

                    int index = 1;
                    foreach (var item in group.OrderByDescending(i => i.WrongCount))
                    {
                        sb.AppendLine($"### 第 {index} 题");
                        sb.AppendLine();
                        sb.AppendLine($"{item.MasteryIcon} **{item.MasteryText}** | 错误 {item.WrongCount} 次 | 复习 {item.ReviewCount} 次");
                        sb.AppendLine();

                        if (!string.IsNullOrWhiteSpace(item.Category))
                        {
                            sb.AppendLine($"**分类:** {item.Category}");
                            sb.AppendLine();
                        }

                        sb.AppendLine("**题目：**");
                        sb.AppendLine();
                        sb.AppendLine(item.Question);
                        sb.AppendLine();

                        sb.AppendLine("**正确答案：**");
                        sb.AppendLine();
                        sb.AppendLine(item.CorrectAnswer);
                        sb.AppendLine();

                        if (!string.IsNullOrWhiteSpace(item.UserAnswer))
                        {
                            sb.AppendLine("**你的答案：**");
                            sb.AppendLine();
                            sb.AppendLine(item.UserAnswer);
                            sb.AppendLine();
                        }

                        if (!string.IsNullOrWhiteSpace(item.Explanation))
                        {
                            sb.AppendLine("**解析：**");
                            sb.AppendLine();
                            sb.AppendLine(item.Explanation);
                            sb.AppendLine();
                        }

                        if (item.TagsList.Count > 0)
                        {
                            sb.AppendLine($"标签: {string.Join(", ", item.TagsList.Select(t => $"`{t}`"))}");
                            sb.AppendLine();
                        }

                        sb.AppendLine("---");
                        sb.AppendLine();
                        index++;
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                _logger.LogInformation("用户 {UserId} 导出错题本 Markdown 成功: {FilePath}", userId, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出错题本 Markdown 失败: {UserId}", userId);
                return false;
            }
        }

        public bool ExportToTextCards(string userId, string filePath, WrongAnswerFilter? filter = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var items = filter != null
                    ? GetWrongAnswers(userId, filter)
                    : GetWrongAnswers(userId);

                var sb = new StringBuilder();
                int cardNumber = 1;

                foreach (var item in items.OrderByDescending(i => i.WrongCount))
                {
                    sb.AppendLine($"===== 错题卡片 {cardNumber:D3} =====");
                    sb.AppendLine();
                    sb.AppendLine("【正面 - 题目】");
                    sb.AppendLine(item.Question);
                    sb.AppendLine();
                    sb.AppendLine("【背面 - 答案】");
                    sb.AppendLine(item.CorrectAnswer);
                    sb.AppendLine();

                    if (!string.IsNullOrWhiteSpace(item.Explanation))
                    {
                        sb.AppendLine("【解析】");
                        sb.AppendLine(item.Explanation);
                        sb.AppendLine();
                    }

                    sb.AppendLine($"科目: {item.Subject} | 分类: {item.Category}");
                    sb.AppendLine($"错误次数: {item.WrongCount} | 掌握程度: {item.MasteryText}");
                    sb.AppendLine();
                    sb.AppendLine();

                    cardNumber++;
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                _logger.LogInformation("用户 {UserId} 导出错题本卡片成功: {FilePath}", userId, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出错题本卡片失败: {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region 私有方法

        private List<WrongAnswerItem> LoadWrongAnswers(string userId)
        {
            var path = GetUserWrongAnswersPath(userId);
            if (!File.Exists(path))
                return new List<WrongAnswerItem>();

            try
            {
                var json = File.ReadAllText(path);
                var items = JsonSerializer.Deserialize<List<WrongAnswerItem>>(json) ?? new List<WrongAnswerItem>();

                bool needsMigration = false;
                foreach (var item in items)
                {
                    if (item.TagsList.Count == 0 && !string.IsNullOrWhiteSpace(item.Tags))
                    {
                        item.TagsList = item.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                        needsMigration = true;
                    }
                }

                if (needsMigration)
                {
                    try
                    {
                        SaveWrongAnswers(userId, items);
                        _logger.LogInformation("用户 {UserId} 错题标签数据已迁移并保存", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "迁移后保存错题数据失败: {UserId}", userId);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载错题数据失败，使用空列表: {UserId}", userId);
                return new List<WrongAnswerItem>();
            }
        }

        private void SaveWrongAnswers(string userId, List<WrongAnswerItem> items)
        {
            var path = GetUserWrongAnswersPath(userId);
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                UnsubscribeFromEvents();
            }

            _disposed = true;
        }

        ~WrongAnswerService()
        {
            Dispose(false);
        }

        #endregion
    }
}
