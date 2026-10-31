using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Managers;
using LearningAssistant.Models.Home;
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
        private readonly IDialogService _dialogService;
        private readonly IHomeSnapshotService _homeSnapshotService;
        private readonly IEventBus? _eventBus;

        private PdfPresenter? _pdfPresenter;
        private IMainView? _view;

        private readonly object _homeRefreshGate = new();
        private Timer? _homeRefreshTimer;
        private int _homeRefreshRunning;
        private bool _disposed;

        private string _currentUserId = Constants.DefaultUserId;
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
            ILearningRecommendationService recommendationService,
            IDialogService dialogService,
            IHomeSnapshotService homeSnapshotService,
            IEventBus? eventBus = null)
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
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _homeSnapshotService = homeSnapshotService ?? throw new ArgumentNullException(nameof(homeSnapshotService));
            _eventBus = eventBus;
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
            _view.OpenUserComparisonClicked += View_OpenUserComparisonClicked;

            // 学习活动事件：用于首页快照的防抖刷新（07 方案 H-1）
            if (_eventBus != null)
            {
                _eventBus.Subscribe<ItemLearnedEvent>(OnItemLearned);
                _eventBus.Subscribe<ItemWrongEvent>(OnItemWrong);
                _eventBus.Subscribe<ReviewDoneEvent>(OnReviewDone);
                _eventBus.Subscribe<PomodoroCompletedEvent>(OnPomodoroCompleted);
            }
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
            _view.OpenUserComparisonClicked -= View_OpenUserComparisonClicked;

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<ItemLearnedEvent>(OnItemLearned);
                _eventBus.Unsubscribe<ItemWrongEvent>(OnItemWrong);
                _eventBus.Unsubscribe<ReviewDoneEvent>(OnReviewDone);
                _eventBus.Unsubscribe<PomodoroCompletedEvent>(OnPomodoroCompleted);
            }
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
            if (_view == null) return;

            try
            {
                // 首页数据统一由 HomeSnapshotService 聚合（07 方案 3.2）：
                // 统计底座/游戏化/笔记/复习/目标的降级逻辑收敛在聚合服务内。
                _view.UpdateHomeSnapshot(_homeSnapshotService.GetSnapshot(_currentUserId));

                // 简洁版首页不展示推荐，跳过无意义的后台请求（07 方案 H-5）
                if (_view.IsDashboardHomeActive)
                {
                    LoadRecommendationsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 Dashboard 统计数据失败");
            }
        }

        /// <summary>
        /// 学习/复习/番茄钟等事件到达后，300ms 防抖刷新首页快照。
        /// </summary>
        private void OnHomeSnapshotChanged()
        {
            if (_disposed) return;

            lock (_homeRefreshGate)
            {
                _homeRefreshTimer ??= new Timer(
                    _ => RefreshHomeSnapshot(),
                    null,
                    Timeout.Infinite,
                    Timeout.Infinite);
                _homeRefreshTimer.Change(HomeRefreshDebounceMilliseconds, Timeout.Infinite);
            }
        }

        private void OnItemLearned(ItemLearnedEvent e) => OnHomeSnapshotChanged();
        private void OnItemWrong(ItemWrongEvent e) => OnHomeSnapshotChanged();
        private void OnReviewDone(ReviewDoneEvent e) => OnHomeSnapshotChanged();
        private void OnPomodoroCompleted(PomodoroCompletedEvent e) => OnHomeSnapshotChanged();

        private void RefreshHomeSnapshot()
        {
            if (_disposed) return;
            if (Interlocked.Exchange(ref _homeRefreshRunning, 1) == 1) return;

            try
            {
                _view?.UpdateHomeSnapshot(_homeSnapshotService.GetSnapshot(_currentUserId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新首页快照失败");
            }
            finally
            {
                Interlocked.Exchange(ref _homeRefreshRunning, 0);
            }
        }

        private const int HomeRefreshDebounceMilliseconds = 300;

        private void LoadRecommendationsAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(300);
                    UpdateRecommendations();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "异步加载推荐数据失败");
                }
            });
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

        /// <summary>
        /// 供外部（如设置窗体关闭后）触发用户列表刷新。
        /// </summary>
        public void RefreshUsers()
        {
            RefreshUserList();
        }

        private void UpdateStatus()
        {
            string ttsStatus = _ttsService.Available ? "TTS 就绪" : "TTS 加载中";
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
                await _dialogService.ShowMessageAsync("错误", $"打开学习窗口失败: {ex.Message}");
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

        private void SaveSession()
        {
            _sessionService.SaveSession(_currentUserId);
        }

        public void Dispose()
        {
            _disposed = true;
            UnsubscribeFromEvents();

            lock (_homeRefreshGate)
            {
                _homeRefreshTimer?.Dispose();
                _homeRefreshTimer = null;
            }

            _logger.LogInformation("MainPresenter disposed");
        }
    }
}