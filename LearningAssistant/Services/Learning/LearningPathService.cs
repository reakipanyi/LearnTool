using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习路径推荐服务实现
    /// 提供学习路径管理和智能推荐功能
    /// </summary>
    public class LearningPathService : ILearningPathService
    {
        private readonly IDataPersistenceService _persistenceService;
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILogger<LearningPathService> _logger;
        private readonly string _pathsDir;
        private readonly object _lock = new object();

        public LearningPathService(
            IDataPersistenceService persistenceService,
            ILearningAnalyticsService analyticsService,
            IWrongAnswerService wrongAnswerService,
            ILogger<LearningPathService> logger)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathsDir = Path.Combine(AppPaths.UsersDir, "learning_paths");
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_pathsDir))
            {
                Directory.CreateDirectory(_pathsDir);
            }
        }

        private string GetUserPathsPath(string userId)
        {
            return Path.Combine(_pathsDir, $"{userId}_paths.json");
        }

        private List<LearningPath> LoadPaths(string userId)
        {
            try
            {
                var path = GetUserPathsPath(userId);
                if (!File.Exists(path))
                    return new List<LearningPath>();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<LearningPath>>(json) ?? new List<LearningPath>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载学习路径数据失败，返回空列表");
                return new List<LearningPath>();
            }
        }

        private void SavePaths(string userId, List<LearningPath> paths)
        {
            try
            {
                var path = GetUserPathsPath(userId);
                var json = JsonSerializer.Serialize(paths, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存学习路径数据失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public void CreatePath(string userId, LearningPath path)
        {
            lock (_lock)
            {
                try
                {
                    var paths = LoadPaths(userId);
                    path.Id = Guid.NewGuid().ToString();
                    path.UserId = userId;
                    path.CreatedAt = DateTime.Now;
                    path.UpdatedAt = DateTime.Now;
                    path.TotalEstimatedMinutes = path.Items.Sum(i => i.EstimatedMinutes);
                    paths.Add(path);
                    SavePaths(userId, paths);
                    _logger.LogInformation($"学习路径已创建: {path.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建学习路径失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void UpdatePath(string userId, LearningPath path)
        {
            lock (_lock)
            {
                try
                {
                    var paths = LoadPaths(userId);
                    var index = paths.FindIndex(p => p.Id == path.Id);
                    if (index >= 0)
                    {
                        path.UpdatedAt = DateTime.Now;
                        path.TotalEstimatedMinutes = path.Items.Sum(i => i.EstimatedMinutes);
                        paths[index] = path;
                        SavePaths(userId, paths);
                        _logger.LogInformation($"学习路径已更新: {path.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新学习路径失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void DeletePath(string userId, string pathId)
        {
            lock (_lock)
            {
                try
                {
                    var paths = LoadPaths(userId);
                    var pathToRemove = paths.FirstOrDefault(p => p.Id == pathId);
                    if (pathToRemove != null)
                    {
                        paths.Remove(pathToRemove);
                        SavePaths(userId, paths);
                        _logger.LogInformation($"学习路径已删除: {pathToRemove.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除学习路径失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public LearningPath? GetPath(string userId, string pathId)
        {
            try
            {
                var paths = LoadPaths(userId);
                return paths.FirstOrDefault(p => p.Id == pathId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取学习路径失败");
                return null;
            }
        }

        /// <inheritdoc/>
        public List<LearningPath> GetAllPaths(string userId)
        {
            try
            {
                var paths = LoadPaths(userId);
                return paths.OrderByDescending(p => p.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取学习路径列表失败");
                return new List<LearningPath>();
            }
        }

        /// <inheritdoc/>
        public LearningPath? GetActivePath(string userId)
        {
            try
            {
                var paths = LoadPaths(userId);
                return paths.FirstOrDefault(p => p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取激活的学习路径失败");
                return null;
            }
        }

        /// <inheritdoc/>
        public void ActivatePath(string userId, string pathId)
        {
            lock (_lock)
            {
                try
                {
                    var paths = LoadPaths(userId);
                    foreach (var path in paths)
                    {
                        path.IsActive = path.Id == pathId;
                        if (path.IsActive && path.StartDate == null)
                        {
                            path.StartDate = DateTime.Now;
                        }
                        path.UpdatedAt = DateTime.Now;
                    }
                    SavePaths(userId, paths);
                    _logger.LogInformation($"学习路径已激活: {pathId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "激活学习路径失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void UpdateItemProgress(string userId, string pathId, string itemId, int progress)
        {
            lock (_lock)
            {
                try
                {
                    var paths = LoadPaths(userId);
                    var path = paths.FirstOrDefault(p => p.Id == pathId);
                    if (path != null)
                    {
                        var item = path.Items.FirstOrDefault(i => i.Id == itemId);
                        if (item != null)
                        {
                            item.Progress = Math.Clamp(progress, 0, 100);
                            item.IsCompleted = item.Progress >= 100;
                            if (item.IsCompleted && item.CompletedAt == null)
                            {
                                item.CompletedAt = DateTime.Now;
                            }
                            path.UpdatedAt = DateTime.Now;
                            SavePaths(userId, paths);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新路径项进度失败");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void MarkItemCompleted(string userId, string pathId, string itemId)
        {
            UpdateItemProgress(userId, pathId, itemId, 100);
        }

        /// <inheritdoc/>
        public List<LearningRecommendation> GetTodayRecommendations(string userId, int count = 5)
        {
            try
            {
                var recommendations = new List<LearningRecommendation>();

                var activePath = GetActivePath(userId);
                if (activePath != null)
                {
                    var nextItem = activePath.Items
                        .Where(i => !i.IsCompleted)
                        .OrderBy(i => i.Order)
                        .FirstOrDefault();

                    if (nextItem != null)
                    {
                        recommendations.Add(new LearningRecommendation
                        {
                            Type = "path",
                            Title = $"继续学习: {nextItem.Title}",
                            Reason = $"当前学习路径「{activePath.Name}」的下一个内容",
                            ContentType = nextItem.ContentType,
                            ContentId = nextItem.Id,
                            Priority = 10,
                            EstimatedMinutes = nextItem.EstimatedMinutes
                        });
                    }
                }

                var wrongCount = _wrongAnswerService.GetWrongAnswerCount(userId);
                if (wrongCount > 0)
                {
                    recommendations.Add(new LearningRecommendation
                    {
                        Type = "review",
                        Title = "复习错题",
                        Reason = $"你有 {wrongCount} 道错题等待复习，巩固薄弱点",
                        ContentType = "wronganswer",
                        Priority = 9,
                        EstimatedMinutes = Math.Min(wrongCount * 2, 30)
                    });
                }

                var todayStats = _analyticsService.GetDailyStatistics(userId, DateTime.Today);
                if (todayStats != null)
                {
                    recommendations.Add(new LearningRecommendation
                    {
                        Type = "goal",
                        Title = "完成今日学习",
                        Reason = $"今日已学习 {todayStats.TotalItems} 项，继续加油！",
                        ContentType = "goal",
                        Priority = 8,
                        EstimatedMinutes = 15
                    });
                }

                recommendations.Add(new LearningRecommendation
                {
                    Type = "explore",
                    Title = "探索新内容",
                    Reason = "尝试新的知识领域，拓宽知识面",
                    ContentType = "general",
                    Priority = 5,
                    EstimatedMinutes = 10
                });

                recommendations.Add(new LearningRecommendation
                {
                    Type = "note",
                    Title = "整理学习笔记",
                    Reason = "定期复习笔记可以加深记忆",
                    ContentType = "note",
                    Priority = 4,
                    EstimatedMinutes = 10
                });

                return recommendations
                    .OrderByDescending(r => r.Priority)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取今日推荐失败");
                return new List<LearningRecommendation>();
            }
        }

        /// <inheritdoc/>
        public LearningPath GenerateRecommendedPath(string userId, string domain, string targetLevel, int days = 30)
        {
            try
            {
                var path = new LearningPath
                {
                    Name = $"{domain}学习路径 - {targetLevel}",
                    Description = $"系统推荐的{domain}学习路径，目标{targetLevel}水平",
                    Goal = $"在{days}天内达到{targetLevel}的{domain}水平",
                    PathType = "recommended",
                    Domain = domain,
                    Level = targetLevel,
                    TargetDate = DateTime.Now.AddDays(days),
                    IsActive = false
                };

                var items = new List<LearningPathItem>();
                int totalItems = Math.Max(10, days / 3);
                int minutesPerItem = Math.Max(5, 30 / Math.Max(1, totalItems / days));

                for (int i = 0; i < totalItems; i++)
                {
                    var difficulty = (int)Math.Ceiling((i + 1) * 5.0 / totalItems);
                    items.Add(new LearningPathItem
                    {
                        Title = $"{domain}学习 - 第{i + 1}阶段",
                        Description = $"第{i + 1}阶段的{domain}学习内容",
                        ContentType = domain,
                        Order = i + 1,
                        DifficultyLevel = difficulty,
                        EstimatedMinutes = minutesPerItem * 3
                    });
                }

                path.Items = items;
                path.TotalEstimatedMinutes = items.Sum(i => i.EstimatedMinutes);

                _logger.LogInformation($"已生成推荐学习路径: {path.Name}");
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成推荐路径失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public string GetNextStageSuggestion(string userId)
        {
            try
            {
                var streak = _analyticsService.GetStudyStreak(userId);
                var wrongCount = _wrongAnswerService.GetWrongAnswerCount(userId);
                var masteredCount = _wrongAnswerService.GetMasteredCount(userId);
                var todayStats = _analyticsService.GetDailyStatistics(userId, DateTime.Today);

                if (streak == 0)
                {
                    return "欢迎开始学习之旅！建议先从你最感兴趣的领域开始，设定一个小目标。";
                }

                if (streak < 7)
                {
                    return "你正在建立学习习惯！建议保持每日学习，尝试使用学习路径功能来规划进度。";
                }

                if (wrongCount > masteredCount && wrongCount > 10)
                {
                    return "你的错题积累较多，建议安排时间集中复习错题本，巩固薄弱知识点。";
                }

                if (todayStats != null && todayStats.CorrectRate >= 0.9)
                {
                    return "太棒了！你的正确率很高，可以考虑挑战更难的内容，或者学习新的知识领域。";
                }

                if (streak >= 30)
                {
                    return $"恭喜你连续学习{streak}天！你已经养成了很好的学习习惯，继续保持！";
                }

                return "学习进展不错！建议设定一个明确的学习目标，使用学习路径功能来系统提升。";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取下一阶段建议失败");
                return "继续保持学习的热情，每天进步一点点！";
            }
        }

        /// <inheritdoc/>
        public List<LearningRecommendation> GetWeakPoints(string userId)
        {
            try
            {
                var weakPoints = new List<LearningRecommendation>();
                var wrongAnswers = _wrongAnswerService.GetWrongAnswers(userId);

                var subjectGroups = wrongAnswers
                    .Where(w => !w.IsMastered)
                    .GroupBy(w => w.Subject)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToList();

                foreach (var group in subjectGroups)
                {
                    if (group.Key == SubjectType.Unknown)
                        continue;

                    var subjectStr = group.Key.ToString();
                    weakPoints.Add(new LearningRecommendation
                    {
                        Type = "weakpoint",
                        Title = $"{subjectStr}薄弱点加强",
                        Reason = $"{subjectStr}有{group.Count()}道错题待掌握",
                        ContentType = subjectStr,
                        Priority = group.Count(),
                        EstimatedMinutes = group.Count() * 3
                    });
                }

                return weakPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取薄弱点失败");
                return new List<LearningRecommendation>();
            }
        }

        /// <inheritdoc/>
        public LearningPathItem? GetNextItem(string userId)
        {
            try
            {
                var activePath = GetActivePath(userId);
                if (activePath == null)
                    return null;

                return activePath.Items
                    .Where(i => !i.IsCompleted)
                    .OrderBy(i => i.Order)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取下一个学习项失败");
                return null;
            }
        }
    }
}
