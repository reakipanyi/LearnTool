using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.KnowledgeGraph;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    public class LearningRecommendationService : ILearningRecommendationService
    {
        private readonly ISpacedRepetitionService _spacedRepetitionService;
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly ILearningPathService _learningPathService;
        private readonly IKnowledgeGraphService? _knowledgeGraphService;
        private readonly IPomodoroService? _pomodoroService;
        private readonly ILogger<LearningRecommendationService> _logger;
        private readonly string _feedbackDir;
        private readonly Dictionary<string, RecommendationWeights> _userWeights = new Dictionary<string, RecommendationWeights>();
        private readonly Dictionary<string, List<string>> _recentCategories = new Dictionary<string, List<string>>();
        private readonly object _lock = new object();

        public LearningRecommendationService(
            ISpacedRepetitionService spacedRepetitionService,
            IWrongAnswerService wrongAnswerService,
            ILearningAnalyticsService analyticsService,
            ILearningPathService learningPathService,
            ILogger<LearningRecommendationService> logger,
            IKnowledgeGraphService? knowledgeGraphService = null,
            IPomodoroService? pomodoroService = null)
        {
            _spacedRepetitionService = spacedRepetitionService ?? throw new ArgumentNullException(nameof(spacedRepetitionService));
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _learningPathService = learningPathService ?? throw new ArgumentNullException(nameof(learningPathService));
            _knowledgeGraphService = knowledgeGraphService;
            _pomodoroService = pomodoroService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _feedbackDir = Path.Combine(AppPaths.UsersDir, "recommendation_feedback");
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_feedbackDir))
            {
                Directory.CreateDirectory(_feedbackDir);
            }
        }

        private string GetUserFeedbackPath(string userId)
        {
            return Path.Combine(_feedbackDir, $"{userId}_feedback.json");
        }

        public List<LearningRecommendation> GetDailyRecommendations(string userId, int count = 6)
        {
            try
            {
                var recommendations = new List<LearningRecommendation>();

                var reviewItems = GenerateReviewRecommendations(userId, count / 2);
                recommendations.AddRange(reviewItems);

                var weakPointItems = GenerateWeakPointRecommendations(userId, count / 3);
                recommendations.AddRange(weakPointItems);

                var pathItems = GeneratePathRecommendations(userId, 1);
                recommendations.AddRange(pathItems);

                var explorationItems = GenerateExplorationRecommendations(userId, count - recommendations.Count);
                recommendations.AddRange(explorationItems);

                foreach (var rec in recommendations)
                {
                    rec.Priority = (int)Math.Round(CalculateRecommendationScore(userId, rec) * 10);
                }

                return recommendations
                    .OrderByDescending(r => r.Priority)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取每日推荐失败，返回默认推荐");
                return GetDefaultRecommendations(count);
            }
        }

        private List<LearningRecommendation> GenerateReviewRecommendations(string userId, int count)
        {
            var recommendations = new List<LearningRecommendation>();

            try
            {
                var dueItems = _spacedRepetitionService.GetItemsDueForReview(userId, DateTime.Today);
                if (dueItems.Count > 0)
                {
                    var urgentItems = dueItems
                        .OrderBy(i => i.NextReviewDate)
                        .Take(Math.Min(count, dueItems.Count))
                        .ToList();

                    foreach (var item in urgentItems)
                    {
                        var urgency = CalculateUrgencyScore(item);
                        recommendations.Add(new LearningRecommendation
                        {
                            Type = "review",
                            Title = $"复习: {TruncateText(item.Content, 20)}",
                            Reason = urgency >= 0.8 ? "即将遗忘，急需复习！" : "到了复习时间，巩固记忆",
                            ContentType = "review",
                            ContentId = item.Id.ToString(),
                            Priority = (int)(urgency * 10),
                            EstimatedMinutes = 3
                        });
                    }
                }

                var wrongAnswers = _wrongAnswerService.GetWrongAnswers(userId, 0, count);
                if (wrongAnswers.Count > 0)
                {
                    recommendations.Add(new LearningRecommendation
                    {
                        Type = "review",
                        Title = $"错题复习（{wrongAnswers.Count}道）",
                        Reason = "攻克错题，补齐知识短板",
                        ContentType = "wronganswer",
                        Priority = 7,
                        EstimatedMinutes = Math.Min(wrongAnswers.Count * 3, 30)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成复习推荐失败");
            }

            return recommendations.Take(count).ToList();
        }

        private List<LearningRecommendation> GenerateWeakPointRecommendations(string userId, int count)
        {
            var recommendations = new List<LearningRecommendation>();

            try
            {
                var weakPoints = GetWeakPoints(userId);
                var severeWeaks = weakPoints
                    .Where(w => w.Severity >= 0.6)
                    .OrderByDescending(w => w.Severity)
                    .Take(count)
                    .ToList();

                foreach (var weak in severeWeaks)
                {
                    recommendations.Add(new LearningRecommendation
                    {
                        Type = "weakpoint",
                        Title = $"强化{weak.CategoryName}",
                        Reason = weak.Suggestion,
                        ContentType = weak.Category,
                        Priority = (int)(weak.Severity * 9) + 1,
                        EstimatedMinutes = 15
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成薄弱点推荐失败");
            }

            return recommendations;
        }

        private List<LearningRecommendation> GeneratePathRecommendations(string userId, int count)
        {
            var recommendations = new List<LearningRecommendation>();

            try
            {
                var nextItem = _learningPathService.GetNextItem(userId);
                if (nextItem != null)
                {
                    var activePath = _learningPathService.GetActivePath(userId);
                    recommendations.Add(new LearningRecommendation
                    {
                        Type = "path",
                        Title = $"继续学习: {nextItem.Title}",
                        Reason = activePath != null
                            ? $"「{activePath.Name}」学习路径的下一个内容"
                            : "继续你的学习旅程",
                        ContentType = nextItem.ContentType,
                        ContentId = nextItem.Id,
                        Priority = 8,
                        EstimatedMinutes = nextItem.EstimatedMinutes
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成路径推荐失败");
            }

            return recommendations.Take(count).ToList();
        }

        private List<LearningRecommendation> GenerateExplorationRecommendations(string userId, int count)
        {
            var recommendations = new List<LearningRecommendation>();

            try
            {
                var categoryStats = _analyticsService.GetCategoryStats(userId);
                var allCategories = new[] { "汉字", "单词", "诗词", "成语", "语法", "阅读理解" };
                var learnedCategories = categoryStats.Keys.ToHashSet();

                var unexplored = allCategories
                    .Where(c => !learnedCategories.Contains(c))
                    .Take(count)
                    .ToList();

                foreach (var category in unexplored)
                {
                    recommendations.Add(new LearningRecommendation
                    {
                        Type = "explore",
                        Title = $"探索{category}",
                        Reason = "尝试新领域，拓宽知识面",
                        ContentType = category,
                        Priority = 4,
                        EstimatedMinutes = 10
                    });
                }

                if (_knowledgeGraphService != null)
                {
                    try
                    {
                        var weakNodes = _knowledgeGraphService.GetWeakNodesAsync(userId, Math.Min(count, 5)).Result;
                        foreach (var node in weakNodes)
                        {
                            recommendations.Add(new LearningRecommendation
                            {
                                Type = "weakpoint",
                                Title = $"巩固知识: {node.Label}",
                                Reason = $"知识图谱检测到掌握度较低 ({Math.Round(node.MasteryLevel * 100)}%)，建议复习",
                                ContentType = node.Category,
                                Priority = (int)(node.MasteryLevel * 10),
                                EstimatedMinutes = 5
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "从知识图谱获取薄弱节点失败");
                    }
                }

                if (_pomodoroService != null)
                {
                    var stats = _pomodoroService.GetStatistics();
                    var todayPomodoros = stats.TodayCount;
                    if (todayPomodoros < 4)
                    {
                        recommendations.Add(new LearningRecommendation
                        {
                            Type = "pomodoro",
                            Title = $"完成第 {todayPomodoros + 1} 个番茄钟",
                            Reason = $"今日已完成 {todayPomodoros} 个番茄钟，建议继续专注学习",
                            ContentType = "pomodoro",
                            Priority = 7,
                            EstimatedMinutes = _pomodoroService.Settings.StudyMinutes
                        });
                    }
                }

                recommendations.Add(new LearningRecommendation
                {
                    Type = "goal",
                    Title = "完成今日目标",
                    Reason = "坚持每日学习，养成好习惯",
                    ContentType = "goal",
                    Priority = 6,
                    EstimatedMinutes = 15
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成探索推荐失败");
            }

            return recommendations.Take(Math.Max(count, 1)).ToList();
        }

        public LearningRecommendation? GetNextItem(string userId)
        {
            var recommendations = GetDailyRecommendations(userId, 3);
            return recommendations.FirstOrDefault();
        }

        public List<WeakPointAnalysis> GetWeakPoints(string userId)
        {
            var result = new List<WeakPointAnalysis>();

            try
            {
                var wrongAnswers = _wrongAnswerService.GetWrongAnswers(userId, 0, 100);
                var categoryStats = _analyticsService.GetCategoryStats(userId);

                var categoryWrongCounts = new Dictionary<string, int>();
                var categoryTotalCounts = new Dictionary<string, int>();

                foreach (var wa in wrongAnswers)
                {
                    var category = string.IsNullOrWhiteSpace(wa.Subject) ? "通用" : wa.Subject;
                    if (!categoryWrongCounts.ContainsKey(category))
                    {
                        categoryWrongCounts[category] = 0;
                        categoryTotalCounts[category] = 0;
                    }
                    categoryWrongCounts[category] += wa.WrongCount;
                    categoryTotalCounts[category] += wa.WrongCount + (wa.CorrectCount > 0 ? wa.CorrectCount : 5);
                }

                foreach (var kvp in categoryStats)
                {
                    if (!categoryTotalCounts.ContainsKey(kvp.Key))
                    {
                        categoryTotalCounts[kvp.Key] = kvp.Value;
                        categoryWrongCounts[kvp.Key] = 0;
                    }
                }

                var iconMap = new Dictionary<string, string>
                {
                    ["汉字"] = "🔤",
                    ["单词"] = "📝",
                    ["诗词"] = "📜",
                    ["成语"] = "🎭",
                    ["语法"] = "📖",
                    ["阅读理解"] = "📚",
                    ["通用"] = "📋"
                };

                foreach (var category in categoryTotalCounts.Keys)
                {
                    int wrong = categoryWrongCounts.TryGetValue(category, out var w) ? w : 0;
                    int total = categoryTotalCounts[category];
                    double errorRate = total > 0 ? (double)wrong / total : 0;

                    double severity = CalculateSeverity(errorRate, total);

                    var suggestion = GenerateWeakPointSuggestion(category, errorRate, severity);
                    var icon = iconMap.TryGetValue(category, out var i) ? i : "📚";

                    result.Add(new WeakPointAnalysis
                    {
                        Category = category,
                        CategoryName = category,
                        ErrorRate = errorRate,
                        TotalCount = total,
                        WrongCount = wrong,
                        Severity = severity,
                        Suggestion = suggestion,
                        Icon = icon
                    });
                }

                return result
                    .OrderByDescending(w => w.Severity)
                    .ThenByDescending(w => w.WrongCount)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析薄弱点失败");
                return result;
            }
        }

        private double CalculateSeverity(double errorRate, int totalCount)
        {
            if (totalCount < 5)
            {
                return errorRate * 0.5;
            }

            double baseSeverity = errorRate;

            double confidenceFactor = Math.Min(1.0, totalCount / 20.0);

            return baseSeverity * (0.5 + 0.5 * confidenceFactor);
        }

        private string GenerateWeakPointSuggestion(string category, double errorRate, double severity)
        {
            if (severity >= 0.8)
                return $"{category}错误率较高，建议重点复习，多做专项练习";
            if (severity >= 0.6)
                return $"{category}有提升空间，建议定期复习巩固";
            if (severity >= 0.4)
                return $"{category}掌握一般，偶尔复习效果更好";
            return $"{category}掌握不错，继续保持！";
        }

        public LearningPathSuggestion GetLearningPathSuggestion(string userId, string domain)
        {
            var suggestion = new LearningPathSuggestion
            {
                Domain = domain,
                CurrentLevel = "初级",
                SuggestedNextLevel = "中级",
                ProgressPercent = 35.0,
                Suggestion = "继续保持学习节奏，你进步很快！",
                EstimatedDaysToNextLevel = 30
            };

            try
            {
                var categoryStats = _analyticsService.GetCategoryStats(userId);
                var totalItems = categoryStats.Values.Sum();
                var weakPoints = GetWeakPoints(userId);
                var avgErrorRate = weakPoints.Count > 0 ? weakPoints.Average(w => w.ErrorRate) : 0.3;

                string currentLevel;
                string nextLevel;
                double progress;
                int daysToNext;

                if (totalItems < 50)
                {
                    currentLevel = "入门";
                    nextLevel = "初级";
                    progress = totalItems / 50.0 * 100;
                    daysToNext = Math.Max(5, 50 - totalItems);
                }
                else if (totalItems < 200)
                {
                    currentLevel = "初级";
                    nextLevel = "中级";
                    progress = (totalItems - 50) / 150.0 * 100;
                    daysToNext = Math.Max(10, (200 - totalItems) / 10);
                }
                else if (totalItems < 500)
                {
                    currentLevel = "中级";
                    nextLevel = "高级";
                    progress = (totalItems - 200) / 300.0 * 100;
                    daysToNext = Math.Max(15, (500 - totalItems) / 15);
                }
                else
                {
                    currentLevel = "高级";
                    nextLevel = "专家";
                    progress = Math.Min(100, (totalItems - 500) / 500.0 * 100);
                    daysToNext = Math.Max(30, (1000 - totalItems) / 10);
                }

                suggestion.CurrentLevel = currentLevel;
                suggestion.SuggestedNextLevel = nextLevel;
                suggestion.ProgressPercent = Math.Round(progress, 1);
                suggestion.EstimatedDaysToNextLevel = daysToNext;

                if (weakPoints.Count > 0)
                {
                    var topWeaks = weakPoints.Take(3).Select(w => w.CategoryName).ToList();
                    suggestion.NextTopics = topWeaks;
                    suggestion.Suggestion = $"建议重点加强 {string.Join("、", topWeaks)} 的学习";
                }
                else
                {
                    suggestion.NextTopics = new List<string> { "深度学习", "拓展应用", "综合练习" };
                    suggestion.Suggestion = "基础扎实，可以尝试更有挑战性的内容";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成学习路径建议失败，使用默认值");
            }

            return suggestion;
        }

        public List<LearningRecommendation> GetReviewPriorities(string userId, int count = 10)
        {
            var priorities = new List<LearningRecommendation>();

            try
            {
                var dueItems = _spacedRepetitionService.GetItemsDueForReview(userId, DateTime.Today.AddDays(7));

                foreach (var item in dueItems.OrderBy(i => i.NextReviewDate).Take(count))
                {
                    var urgency = CalculateUrgencyScore(item);
                    priorities.Add(new LearningRecommendation
                    {
                        Type = "review",
                        Title = TruncateText(item.Content, 25),
                        Reason = urgency >= 0.9 ? "非常紧急，即将遗忘" :
                                 urgency >= 0.7 ? "建议尽快复习" :
                                 "即将到期，安排复习",
                        ContentType = "review",
                        ContentId = item.Id.ToString(),
                        Priority = (int)(urgency * 10),
                        EstimatedMinutes = 2
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取复习优先级失败");
            }

            return priorities;
        }

        public double CalculateRecommendationScore(string userId, LearningRecommendation item)
        {
            var weights = GetWeights(userId);

            double urgency = CalculateUrgencyForRecommendation(userId, item);
            double weakness = CalculateWeaknessForRecommendation(userId, item);
            double freshness = CalculateFreshnessForRecommendation(userId, item);
            double variety = CalculateVarietyForRecommendation(userId, item);

            double score =
                weights.UrgencyWeight * urgency +
                weights.WeaknessWeight * weakness +
                weights.FreshnessWeight * freshness +
                weights.VarietyWeight * variety;

            return Math.Clamp(score, 0, 1);
        }

        private double CalculateUrgencyForRecommendation(string userId, LearningRecommendation item)
        {
            if (item.Type == "review")
                return 0.9;
            if (item.Type == "weakpoint")
                return 0.7;
            if (item.Type == "path")
                return 0.6;
            return 0.3;
        }

        private double CalculateWeaknessForRecommendation(string userId, LearningRecommendation item)
        {
            if (item.Type == "weakpoint")
                return item.Priority / 10.0;

            try
            {
                var weakPoints = GetWeakPoints(userId);
                var matchingWeak = weakPoints.FirstOrDefault(w =>
                    w.Category.Equals(item.ContentType, StringComparison.OrdinalIgnoreCase));
                return matchingWeak?.Severity ?? 0.3;
            }
            catch
            {
                return 0.3;
            }
        }

        private double CalculateFreshnessForRecommendation(string userId, LearningRecommendation item)
        {
            if (!_recentCategories.TryGetValue(userId, out var recent))
                return 0.8;

            bool isRecent = recent.Any(r => r.Equals(item.ContentType, StringComparison.OrdinalIgnoreCase));
            return isRecent ? 0.3 : 0.9;
        }

        private double CalculateVarietyForRecommendation(string userId, LearningRecommendation item)
        {
            try
            {
                var categoryStats = _analyticsService.GetCategoryStats(userId);
                if (categoryStats.Count == 0) return 0.8;

                var total = categoryStats.Values.Sum();
                if (total == 0) return 0.8;

                var matchingCategory = categoryStats
                    .FirstOrDefault(kvp => kvp.Key.Equals(item.ContentType, StringComparison.OrdinalIgnoreCase));

                if (matchingCategory.Key == null) return 0.9;

                double proportion = (double)matchingCategory.Value / total;
                return 1.0 - Math.Min(0.8, proportion);
            }
            catch
            {
                return 0.5;
            }
        }

        private double CalculateUrgencyScore(ReviewItem item)
        {
            var timeUntilDue = item.NextReviewDate - DateTime.Now;

            if (timeUntilDue.TotalDays < 0)
                return 1.0;
            if (timeUntilDue.TotalHours < 1)
                return 0.95;
            if (timeUntilDue.TotalHours < 6)
                return 0.85;
            if (timeUntilDue.TotalDays < 1)
                return 0.7;
            if (timeUntilDue.TotalDays < 3)
                return 0.5;
            if (timeUntilDue.TotalDays < 7)
                return 0.3;

            return 0.1;
        }

        public void RecordFeedback(string userId, string recommendationId, bool isInterested)
        {
            try
            {
                lock (_lock)
                {
                    var feedback = LoadFeedback(userId);
                    feedback[recommendationId] = isInterested;
                    SaveFeedback(userId, feedback);

                    if (_userWeights.TryGetValue(userId, out var weights))
                    {
                        if (isInterested)
                        {
                            weights.WeaknessWeight = Math.Min(0.5, weights.WeaknessWeight + 0.02);
                        }
                        else
                        {
                            weights.WeaknessWeight = Math.Max(0.1, weights.WeaknessWeight - 0.01);
                        }

                        double total = weights.UrgencyWeight + weights.WeaknessWeight + weights.FreshnessWeight + weights.VarietyWeight;
                        weights.UrgencyWeight /= total;
                        weights.WeaknessWeight /= total;
                        weights.FreshnessWeight /= total;
                        weights.VarietyWeight /= total;
                    }
                }

                _logger.LogInformation("记录推荐反馈: {UserId}, {RecommendationId}, {IsInterested}",
                    userId, recommendationId, isInterested);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录推荐反馈失败");
            }
        }

        public RecommendationWeights GetWeights(string userId)
        {
            lock (_lock)
            {
                if (_userWeights.TryGetValue(userId, out var weights))
                {
                    return new RecommendationWeights
                    {
                        UrgencyWeight = weights.UrgencyWeight,
                        WeaknessWeight = weights.WeaknessWeight,
                        FreshnessWeight = weights.FreshnessWeight,
                        VarietyWeight = weights.VarietyWeight
                    };
                }

                return new RecommendationWeights();
            }
        }

        public void AdjustWeights(string userId, RecommendationWeights weights)
        {
            lock (_lock)
            {
                _userWeights[userId] = new RecommendationWeights
                {
                    UrgencyWeight = weights.UrgencyWeight,
                    WeaknessWeight = weights.WeaknessWeight,
                    FreshnessWeight = weights.FreshnessWeight,
                    VarietyWeight = weights.VarietyWeight
                };
            }
        }

        private Dictionary<string, bool> LoadFeedback(string userId)
        {
            try
            {
                var path = GetUserFeedbackPath(userId);
                if (!File.Exists(path))
                    return new Dictionary<string, bool>();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
            }
            catch
            {
                return new Dictionary<string, bool>();
            }
        }

        private void SaveFeedback(string userId, Dictionary<string, bool> feedback)
        {
            try
            {
                var path = GetUserFeedbackPath(userId);
                var json = JsonSerializer.Serialize(feedback, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存反馈数据失败");
            }
        }

        private List<LearningRecommendation> GetDefaultRecommendations(int count)
        {
            var defaults = new List<LearningRecommendation>
            {
                new LearningRecommendation
                {
                    Type = "learn",
                    Title = "开始学习",
                    Reason = "开启今日学习之旅",
                    ContentType = "general",
                    Priority = 8,
                    EstimatedMinutes = 15
                },
                new LearningRecommendation
                {
                    Type = "review",
                    Title = "复习旧知识",
                    Reason = "温故而知新",
                    ContentType = "review",
                    Priority = 7,
                    EstimatedMinutes = 10
                }
            };

            return defaults.Take(count).ToList();
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength) + "...";
        }
    }
}
