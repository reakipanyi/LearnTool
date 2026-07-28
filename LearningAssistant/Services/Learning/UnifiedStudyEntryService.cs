using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Services.Favorites;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 统一学习入口服务实现（P-001）
    /// <para>适配器/门面模式：聚合错题本、收藏夹、学习路径，统一转换为 LearningItem。</para>
    /// <para>安全重构：不修改任何现有服务，只读取和转换数据。</para>
    /// </summary>
    public class UnifiedStudyEntryService : IUnifiedStudyEntryService
    {
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly IFavoritesService _favoritesService;
        private readonly ILearningPathService _learningPathService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly ILogger<UnifiedStudyEntryService>? _logger;

        public UnifiedStudyEntryService(
            IWrongAnswerService wrongAnswerService,
            IFavoritesService favoritesService,
            ILearningPathService learningPathService,
            IContentLoaderService contentLoaderService,
            ILogger<UnifiedStudyEntryService>? logger = null)
        {
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _learningPathService = learningPathService ?? throw new ArgumentNullException(nameof(learningPathService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _logger = logger;
        }

        /// <inheritdoc/>
        public List<StudySourceInfo> GetAvailableSources(string userId)
        {
            var sources = new List<StudySourceInfo>();

            // 1. 错题本
            try
            {
                var wrongCount = _wrongAnswerService.GetWrongAnswerCount(userId);
                if (wrongCount > 0)
                {
                    sources.Add(new StudySourceInfo
                    {
                        Id = "wrong_answer",
                        SourceType = StudySourceType.WrongAnswer,
                        DisplayName = "错题本复习",
                        ItemCount = wrongCount,
                        Description = $"共 {wrongCount} 道错题待复习"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "获取错题本来源失败");
            }

            // 2. 收藏夹
            try
            {
                var favItems = _favoritesService.GetItems(userId);
                if (favItems.Count > 0)
                {
                    sources.Add(new StudySourceInfo
                    {
                        Id = "favorites",
                        SourceType = StudySourceType.Favorite,
                        DisplayName = "收藏夹复习",
                        ItemCount = favItems.Count,
                        Description = $"共 {favItems.Count} 个收藏项"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "获取收藏夹来源失败");
            }

            // 3. 学习路径
            try
            {
                var paths = _learningPathService.GetAllPaths(userId);
                foreach (var path in paths.Where(p => p.IsActive))
                {
                    var pathSource = new StudySourceInfo
                    {
                        Id = $"path_{path.Id}",
                        SourceType = StudySourceType.LearningPath,
                        DisplayName = path.Name,
                        ItemCount = path.Items?.Count ?? 0,
                        Description = path.Description ?? string.Empty
                    };
                    sources.Add(pathSource);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "获取学习路径来源失败");
            }

            return sources;
        }

        /// <inheritdoc/>
        public int GetSourceItemCount(StudySourceInfo source, string userId)
        {
            try
            {
                return source.SourceType switch
                {
                    StudySourceType.WrongAnswer => _wrongAnswerService.GetWrongAnswerCount(userId),
                    StudySourceType.Favorite => _favoritesService.GetItems(userId).Count,
                    StudySourceType.LearningPath => GetLearningPathItemCount(source, userId),
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "获取来源内容数量失败: {SourceType}", source.SourceType);
                return 0;
            }
        }

        /// <inheritdoc/>
        public List<LearningItem> GetItemsFromSource(StudySourceInfo source, string userId, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var items = source.SourceType switch
                {
                    StudySourceType.WrongAnswer => GetWrongAnswerItems(userId),
                    StudySourceType.Favorite => GetFavoriteItems(userId),
                    StudySourceType.LearningPath => GetLearningPathItems(source, userId),
                    _ => new List<LearningItem>()
                };

                // 分页
                return items
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从来源获取学习项失败: {SourceType}, {SourceId}", source.SourceType, source.Id);
                return new List<LearningItem>();
            }
        }

        /// <inheritdoc/>
        public LearningContext? TryCreateContext(StudySourceInfo source, string userId, LearningModeType mode = LearningModeType.Study)
        {
            try
            {
                if (source.SourceType == StudySourceType.Normal && source.Subject.HasValue && source.SubCategory.HasValue)
                {
                    return new LearningContext(
                        userId,
                        source.Subject.Value,
                        source.SubCategory.Value,
                        source.WordBankFile ?? string.Empty,
                        mode,
                        SortOrderType.Sequential
                    );
                }

                if (source.SourceType == StudySourceType.WrongAnswer && source.Subject.HasValue && source.SubCategory.HasValue)
                {
                    return new LearningContext(
                        userId,
                        source.Subject.Value,
                        source.SubCategory.Value,
                        source.WordBankFile ?? string.Empty,
                        mode,
                        SortOrderType.Sequential
                    );
                }

                // 收藏夹和学习路径通常跨分类，无法创建标准 Context
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "创建 LearningContext 失败: {SourceId}", source.Id);
                return null;
            }
        }

        #region 私有转换方法

        private List<LearningItem> GetWrongAnswerItems(string userId)
        {
            var wrongAnswers = _wrongAnswerService.GetWrongAnswers(userId);
            return wrongAnswers.Select(w => new LearningItem
            {
                Id = w.Id,
                Subject = w.Subject,
                SubCategory = w.Category,
                MainContent = w.Question,
                ExtendedProperties = JsonSerializer.Serialize(new
                {
                    source = "wrong_answer",
                    correctAnswer = w.CorrectAnswer,
                    userAnswer = w.UserAnswer
                }),
                Status = LearningStatus.New
            }).ToList();
        }

        private List<LearningItem> GetFavoriteItems(string userId)
        {
            var favorites = _favoritesService.GetItems(userId);
            return favorites.Select(f => new LearningItem
            {
                Id = f.Id,
                Subject = ParseSubject(f.Subject),
                SubCategory = ParseSubCategory(f.SubCategory),
                MainContent = !string.IsNullOrEmpty(f.Title) ? f.Title : (f.Content ?? string.Empty),
                ExtendedProperties = JsonSerializer.Serialize(new
                {
                    source = "favorite",
                    content = f.Content,
                    answer = f.Answer,
                    description = f.Description,
                    type = f.Type.ToString()
                }),
                Status = LearningStatus.New
            }).ToList();
        }

        private List<LearningItem> GetLearningPathItems(StudySourceInfo source, string userId)
        {
            var pathId = source.Id.StartsWith("path_") ? source.Id.Substring(5) : source.Id;
            var path = _learningPathService.GetPath(userId, pathId);
            if (path?.Items == null) return new List<LearningItem>();

            return path.Items
                .OrderBy(i => i.Order)
                .Select(i => new LearningItem
                {
                    Id = i.Id,
                    Subject = SubjectType.English,
                    SubCategory = SubCategoryType.EnglishWord,
                    MainContent = i.Title,
                    ExtendedProperties = JsonSerializer.Serialize(new
                    {
                        source = "learning_path",
                        pathId = pathId,
                        description = i.Description,
                        contentType = i.ContentType,
                        difficultyLevel = i.DifficultyLevel,
                        order = i.Order,
                        contentIds = i.ContentIds
                    }),
                    Status = i.IsCompleted ? LearningStatus.Known : LearningStatus.New
                }).ToList();
        }

        private int GetLearningPathItemCount(StudySourceInfo source, string userId)
        {
            var pathId = source.Id.StartsWith("path_") ? source.Id.Substring(5) : source.Id;
            var path = _learningPathService.GetPath(userId, pathId);
            return path?.Items?.Count ?? 0;
        }

        private static SubjectType ParseSubject(string? subject)
        {
            if (string.IsNullOrEmpty(subject)) return SubjectType.English;
            return Enum.TryParse<SubjectType>(subject, true, out var result) ? result : SubjectType.English;
        }

        private static SubCategoryType ParseSubCategory(string? subCategory)
        {
            if (string.IsNullOrEmpty(subCategory)) return SubCategoryType.EnglishWord;
            return Enum.TryParse<SubCategoryType>(subCategory, true, out var result) ? result : SubCategoryType.EnglishWord;
        }

        #endregion
    }
}
