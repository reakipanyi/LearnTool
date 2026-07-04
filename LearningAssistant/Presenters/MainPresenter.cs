using LearningAssistant.Common;
using LearningAssistant.Managers;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Cache;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Presenters
{
    public class MainPresenter : IDisposable
    {
        private readonly ILogger<MainPresenter> _logger;
        private readonly IUserSessionService _sessionService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IExportService _exportService;
        private readonly ITTSService _ttsService;
        private readonly ICacheService _cacheService;
        private readonly IWindowManager _windowManager;
        private readonly IDataPersistenceService _persistenceService;
        private readonly IGamificationService _gamificationService;
        private readonly ILearningRecommendationService _recommendationService;

        private PdfPresenter? _pdfPresenter;
        private IMainView? _view;

        private string _currentUserId = "Default";
        private UserProfile? _currentUserProfile;

        public event EventHandler? OnOpenSettings;
        public event EventHandler? OnOpenEditor;

        public MainPresenter(
            ILogger<MainPresenter> logger,
            IUserSessionService sessionService,
            IContentLoaderService contentLoaderService,
            IExportService exportService,
            ITTSService ttsService,
            ICacheService cacheService,
            IWindowManager windowManager,
            IDataPersistenceService persistenceService,
            IGamificationService gamificationService,
            ILearningRecommendationService recommendationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _gamificationService = gamificationService ?? throw new ArgumentNullException(nameof(gamificationService));
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
            _logger.LogInformation("MainPresenter initialized");
        }

        public void SetPdfPresenter(PdfPresenter pdfPresenter)
        {
            _pdfPresenter = pdfPresenter;
        }

        public void SetView(IMainView view)
        {
            if (_view == view)
                return;

            UnsubscribeFromEvents();
            _view = view;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_view == null)
                return;

            _view.UserChanged += View_UserChanged;
            _view.OpenLearningWindowClicked += View_OpenLearningWindowClicked;
            _view.OpenSettingsClicked += View_OpenSettingsClicked;
            _view.OpenEditorClicked += View_OpenEditorClicked;
            _view.TabChanged += View_TabChanged;
            _view.NewUserClicked += View_NewUserClicked;
            _view.OpenUserComparisonClicked += View_OpenUserComparisonClicked;
        }

        private void UnsubscribeFromEvents()
        {
            if (_view == null)
                return;

            _view.UserChanged -= View_UserChanged;
            _view.OpenLearningWindowClicked -= View_OpenLearningWindowClicked;
            _view.OpenSettingsClicked -= View_OpenSettingsClicked;
            _view.OpenEditorClicked -= View_OpenEditorClicked;
            _view.TabChanged -= View_TabChanged;
            _view.NewUserClicked -= View_NewUserClicked;
            _view.OpenUserComparisonClicked -= View_OpenUserComparisonClicked;
        }

        public void Initialize()
        {
            if (_view == null)
                return;

            _logger.LogInformation("Initializing MainPresenter");
            LoadSession();
            LoadUserProfile();
            RefreshUserList();
            UpdateStatus();
        }

        private void LoadSession()
        {
            _currentUserId = _sessionService.LoadSession();
            _view.SelectedUser = _currentUserId;
            LoadUserProfile();
        }

        private void LoadUserProfile()
        {
            try
            {
                _gamificationService.Load(_currentUserId);
                _currentUserProfile = _persistenceService.LoadUserProfile(_currentUserId);
                if (_currentUserProfile != null)
                {
                    _currentUserProfile.ResetDailyStats();
                    UpdateStreakInfo();
                    UpdateDashboardStats();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户资料失败");
            }
        }

        private void UpdateStreakInfo()
        {
            if (_currentUserProfile != null && _view != null)
            {
                var summary = _currentUserProfile.GetStudyStatsSummary();
                _view.UpdateStreakInfo(_currentUserProfile.ConsecutiveStudyDays, summary);

                // 更新学习摘要
                var progress = _currentUserProfile.LearningProgress;
                var totalKnown = progress.CategoryProgresses.Values.Sum(cp => cp.KnownItems.Count);
                var totalUnknown = progress.CategoryProgresses.Values.Sum(cp => cp.UnknownItems.Count);
                var totalCorrect = progress.CategoryProgresses.Values.Sum(cp => cp.CorrectCount);
                var totalTest = progress.CategoryProgresses.Values.Sum(cp => cp.TotalTestCount);

                var accuracy = totalTest > 0 ? (double)totalCorrect / totalTest * 100 : 0;

                var progressSummary = $"已掌握 {totalKnown} 个项目\n" +
                                     $"待学习 {totalUnknown} 个项目\n" +
                                     $"正确率 {accuracy:F1}%";
                _view.ProgressSummary = progressSummary;
            }
        }

        private void UpdateDashboardStats()
        {
            if (_currentUserProfile == null || _view == null) return;

            try
            {
                var challenges = _gamificationService.GetDailyChallenges().ToList();
                int completedChallenges = challenges.Count(c => c.Completed);
                int totalChallenges = challenges.Count;

                int noteCount = 0;
                int todayNewNotes = 0;

                _view.UpdateDashboardStats(
                    _currentUserProfile.TodayStudyTimeMinutes,
                    _currentUserProfile.ConsecutiveStudyDays,
                    _gamificationService.XP,
                    _gamificationService.CurrentLevel,
                    _gamificationService.XPToNextLevel,
                    completedChallenges,
                    totalChallenges,
                    noteCount,
                    todayNewNotes);

                UpdateRecommendations();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 Dashboard 统计数据失败");
            }
        }

        private void UpdateRecommendations()
        {
            if (_view == null) return;

            try
            {
                var recommendations = _recommendationService.GetDailyRecommendations(_currentUserId, 5);
                _view.UpdateRecommendations(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐数据失败");
            }
        }

        private void RefreshUserList()
        {
            var users = _sessionService.GetUserList();
            _view.RefreshUserList(users);
        }

        private void UpdateStatus()
        {
            string ttsStatus = _ttsService.Available ? "QwenTTS 在线" : "TTS 未配置";
            string aiStatus = "AI服务就绪";
            string cacheStatus = $"缓存: {_cacheService.Count} 条";
            _view.StatusText = $"{ttsStatus} | {aiStatus} | {cacheStatus}";
        }

        private void View_UserChanged(object? sender, EventArgs e)
        {
            _currentUserId = _view.SelectedUser;
            LoadUserProfile();
            SaveSession();
        }

        private async void View_OpenLearningWindowClicked(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Open learning window clicked");
                SaveSession();

                string language = Constants.Language.Chinese;
                string subCategory = Constants.SubCategory.ChineseCharacter;

                if (_currentUserProfile != null)
                {
                    var progress = _currentUserProfile.LearningProgress;

                    if (progress.CategoryProgresses.Any())
                    {
                        var lastCategory = progress.CategoryProgresses.Values
                            .OrderByDescending(cp => cp.LastTestDate)
                            .FirstOrDefault();

                        if (lastCategory != null)
                        {
                            subCategory = lastCategory.CategoryName;
                            language = GetLanguageFromCategory(subCategory);
                            _logger.LogInformation("Resuming learning from last session: Language={Language}, Category={Category}", language, subCategory);
                        }
                    }
                }

                await _windowManager.OpenLearningWindowAsync(_currentUserId, language, subCategory, string.Empty, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open learning window");
                MessageBox.Show($"打开学习窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetLanguageFromCategory(string category)
        {
            if (category.Contains("English", StringComparison.OrdinalIgnoreCase))
                return Constants.Language.English;
            return Constants.Language.Chinese;
        }

        private void View_OpenSettingsClicked(object? sender, EventArgs e)
        {
            OnOpenSettings?.Invoke(this, EventArgs.Empty);
        }

        private void View_OpenEditorClicked(object? sender, EventArgs e)
        {
            OnOpenEditor?.Invoke(this, EventArgs.Empty);
        }

        private void View_TabChanged(object? sender, EventArgs e)
        {
            SaveSession();
        }

        private void View_NewUserClicked(object? sender, EventArgs e)
        {
            CreateNewUser();
        }

        private void View_OpenUserComparisonClicked(object? sender, EventArgs e)
        {
            LoadUserComparison();
        }

        private void LoadUserComparison()
        {
            try
            {
                var users = _sessionService.GetUserList();
                if (users.Count < 2)
                {
                    _view?.ShowMessage("对比功能需要至少2个用户");
                    return;
                }

                var comparisonData = new List<UserComparisonData>();
                foreach (var userId in users)
                {
                    var profile = _persistenceService.LoadUserProfile(userId);
                    if (profile == null) continue;

                    profile.ResetDailyStats();

                    var progress = profile.LearningProgress;
                    var totalKnown = progress.CategoryProgresses.Values.Sum(cp => cp.KnownItems.Count);
                    var totalCorrect = progress.CategoryProgresses.Values.Sum(cp => cp.CorrectCount);
                    var totalTest = progress.CategoryProgresses.Values.Sum(cp => cp.TotalTestCount);
                    var accuracy = totalTest > 0 ? (double)totalCorrect / totalTest * 100 : 0;

                    // 获取总词汇量
                    var firstCategory = progress.CategoryProgresses.Keys.FirstOrDefault();
                    SubjectSubCategoryMapping.TryParseSubCategory(firstCategory ?? "ChineseCharacter", out var subCategory);
                    var subject = SubjectSubCategoryMapping.GetSubject(subCategory);
                    var context = new LearningContext(userId, subject, subCategory);
                    var totalItems = _contentLoaderService.LoadItems(context).Count;

                    comparisonData.Add(new Views.UserComparisonData
                    {
                        UserId = userId,
                        ConsecutiveStudyDays = profile.ConsecutiveStudyDays,
                        TodayStudyTimeMinutes = profile.TodayStudyTimeMinutes,
                        AccuracyRate = accuracy,
                        KnownItemsCount = totalKnown,
                        TotalStudyTimeMinutes = profile.TotalStudyTimeMinutes,
                        TotalItems = totalItems,
                        AchievementCount = profile.UnlockedAchievements?.Count ?? 0
                    });
                }

                _logger.LogInformation("Loaded user comparison data for {UserCount} users", comparisonData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user comparison data");
                _view?.ShowMessage($"加载对比数据失败：{ex.Message}");
            }
        }

        private void CreateNewUser()
        {
            try
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox("请输入新玩家名称:", "新建玩家", "");
                if (string.IsNullOrWhiteSpace(input))
                    return;

                var userId = input.Trim();

                if (_sessionService.GetUserList().Contains(userId))
                {
                    _view?.ShowMessage("该玩家名称已存在，请使用其他名称！");
                    return;
                }

                _persistenceService.CreateUserProfile(userId, userId);
                RefreshUserList();
                _view.SelectedUser = userId;
                _view?.ShowMessage($"玩家 \"{userId}\" 创建成功！");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建新玩家失败");
                _view?.ShowMessage($"创建玩家失败：{ex.Message}");
            }
        }

        private void SaveSession()
        {
            _sessionService.SaveSession(_currentUserId);
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
            _logger.LogInformation("MainPresenter disposed");
        }
    }
}