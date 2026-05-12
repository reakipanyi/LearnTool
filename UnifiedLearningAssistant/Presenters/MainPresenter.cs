using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services;
using UnifiedLearningAssistant.Services.Cache;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Services.Persistence;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Presenters
{
    public class MainPresenter : IDisposable
    {
        private readonly ILogger<MainPresenter> _logger;
        private readonly IDataPersistenceService _persistenceService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly ITTSService _ttsService;
        private readonly ICacheService _cacheService;
        private readonly IWindowManager _windowManager;
        // 新增功能：PDF生词本联动 - 添加PDF Presenter引用
        private PdfPresenter? _pdfPresenter;
        private IMainView? _view;

        private string _currentUserId = "Guest";
        private string _currentLanguage = Constants.Language.Chinese;
        private string _currentSubCategory = Constants.SubCategory.ChineseCharacter;
        private string _currentMode = Constants.LearningMode.Study;
        private string _currentWordBankFile = string.Empty;
        private string _currentSortOrder = Constants.SortOrder.Sequential;

        public event EventHandler<LearningStartEventArgs>? OnStartLearning;
        public event EventHandler? OnOpenSettings;
        public event EventHandler? OnOpenEditor;
        public event EventHandler? OnOpenStatistics;

        public MainPresenter(
            ILogger<MainPresenter> logger,
            IDataPersistenceService persistenceService,
            IContentLoaderService contentLoaderService,
            ITTSService ttsService,
            ICacheService cacheService,
            IWindowManager windowManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _logger.LogInformation("MainPresenter initialized");
        }

        // 新增功能：PDF生词本联动 - 设置PDF Presenter
        public void SetPdfPresenter(PdfPresenter pdfPresenter)
        {
            _pdfPresenter = pdfPresenter;
            UpdatePdfPresenterConfig();
        }

        // 新增功能：PDF生词本联动 - 更新PDF Presenter的配置
        private void UpdatePdfPresenterConfig()
        {
            _pdfPresenter?.SetCurrentUserAndConfig(_currentUserId, _currentLanguage, _currentSubCategory);
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
            _view.LanguageChanged += View_LanguageChanged;
            _view.SubCategoryChanged += View_SubCategoryChanged;
            _view.ModeChanged += View_ModeChanged;
            _view.WordBankChanged += View_WordBankChanged;
            _view.SortOrderChanged += View_SortOrderChanged;
            _view.StartLearningClicked += View_StartLearningClicked;
            _view.ContinueLearningClicked += View_ContinueLearningClicked;
            _view.OpenSettingsClicked += View_OpenSettingsClicked;
            _view.OpenEditorClicked += View_OpenEditorClicked;
            _view.OpenStatisticsClicked += View_OpenStatisticsClicked;
            // 新增功能：错题本导出 - 订阅导出事件
            _view.ExportErrorBookClicked += View_ExportErrorBookClicked;
            _view.TabChanged += View_TabChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (_view == null)
                return;

            _view.UserChanged -= View_UserChanged;
            _view.LanguageChanged -= View_LanguageChanged;
            _view.SubCategoryChanged -= View_SubCategoryChanged;
            _view.ModeChanged -= View_ModeChanged;
            _view.WordBankChanged -= View_WordBankChanged;
            _view.SortOrderChanged -= View_SortOrderChanged;
            _view.StartLearningClicked -= View_StartLearningClicked;
            _view.ContinueLearningClicked -= View_ContinueLearningClicked;
            _view.OpenSettingsClicked -= View_OpenSettingsClicked;
            _view.OpenEditorClicked -= View_OpenEditorClicked;
            _view.OpenStatisticsClicked -= View_OpenStatisticsClicked;
            // 新增功能：错题本导出 - 取消订阅导出事件
            _view.ExportErrorBookClicked -= View_ExportErrorBookClicked;
            _view.TabChanged -= View_TabChanged;
        }

        public void Initialize()
        {
            if (_view == null)
                return;

            _logger.LogInformation("Initializing MainPresenter");
            LoadSession();
            RefreshUserList();
            RefreshSubCategories();
            RefreshWordBankFiles();
            UpdateProgressSummary();
            UpdateStatus();
        }

        private void LoadSession()
        {
            var session = _persistenceService.LoadSession();
            if (!string.IsNullOrWhiteSpace(session.CurrentUserId))
            {
                _currentUserId = session.CurrentUserId;
                _view.SelectedUser = _currentUserId;
            }
        }

        private void RefreshUserList()
        {
            var users = _persistenceService.GetUserIds();
            if (!users.Any())
            {
                _persistenceService.CreateUserProfile("Guest", "访客");
                users = new List<string> { "Guest" };
            }
            _view.RefreshUserList(users);
        }

        private void RefreshSubCategories()
        {
            var subCats = _contentLoaderService.GetSubCategories(_currentLanguage);
            _view.RefreshSubCategories(subCats);
            if (subCats.Any())
            {
                _currentSubCategory = subCats.First();
                _view.SelectedSubCategory = _currentSubCategory;
            }
        }

        private void RefreshWordBankFiles()
        {
            var files = _contentLoaderService.GetWordBankFiles(_currentSubCategory);
            _view.RefreshWordBankFiles(files);
            var defaultFile = _contentLoaderService.GetDefaultWordBankFile(_currentSubCategory);
            if (!string.IsNullOrWhiteSpace(defaultFile))
            {
                _currentWordBankFile = defaultFile;
                _view.SelectedWordBankFile = _currentWordBankFile;
            }
        }

        private void UpdateProgressSummary()
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            var progress = profile.LearningProgress;

            int totalKnown = 0;
            int totalUnknown = 0;
            double accuracy = 0;

            if (progress.CategoryProgresses.TryGetValue(_currentSubCategory, out var catProgress))
            {
                totalKnown = catProgress.KnownItems.Count;
                totalUnknown = catProgress.UnknownItems.Count;
                if (catProgress.TotalTestCount > 0)
                {
                    accuracy = (double)catProgress.CorrectCount / catProgress.TotalTestCount * 100;
                }
            }

            _view.ProgressSummary = $"玩家: {profile.UserName}\n" +
                $"品类: {_currentLanguage} > {_currentSubCategory}\n" +
                $"已掌握: {totalKnown} | 未掌握: {totalUnknown}\n" +
                $"正确率: {accuracy:F1}%";
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
            UpdateProgressSummary();
            SaveSession();
            UpdatePdfPresenterConfig();
        }

        private void View_LanguageChanged(object? sender, EventArgs e)
        {
            _currentLanguage = _view.SelectedLanguage;
            RefreshSubCategories();
            UpdateProgressSummary();
            UpdatePdfPresenterConfig();
        }

        private void View_SubCategoryChanged(object? sender, EventArgs e)
        {
            _currentSubCategory = _view.SelectedSubCategory;
            RefreshWordBankFiles();
            UpdateProgressSummary();
            UpdatePdfPresenterConfig();
        }

        private void View_ModeChanged(object? sender, EventArgs e)
        {
            _currentMode = _view.SelectedMode;
        }

        private void View_WordBankChanged(object? sender, EventArgs e)
        {
            _currentWordBankFile = _view.SelectedWordBankFile;
        }

        private void View_SortOrderChanged(object? sender, EventArgs e)
        {
            _currentSortOrder = _view.SelectedSortOrder;
        }

        private void View_StartLearningClicked(object? sender, EventArgs e)
        {
            _logger.LogInformation("Start learning clicked");
            StartLearning(false);
        }

        private void View_ContinueLearningClicked(object? sender, EventArgs e)
        {
            _logger.LogInformation("Continue learning clicked");
            StartLearning(true);
        }

        private void StartLearning(bool continueMode)
        {
            SaveSession();
            OnStartLearning?.Invoke(this, new LearningStartEventArgs
            {
                UserId = _currentUserId,
                Language = _currentLanguage,
                SubCategory = _currentSubCategory,
                WordBankFile = _currentWordBankFile,
                Mode = _currentMode,
                SortOrder = _currentSortOrder,
                ContinueMode = continueMode
            });
        }

        private void View_OpenSettingsClicked(object? sender, EventArgs e)
        {
            OnOpenSettings?.Invoke(this, EventArgs.Empty);
        }

        private void View_OpenEditorClicked(object? sender, EventArgs e)
        {
            OnOpenEditor?.Invoke(this, EventArgs.Empty);
        }

        private void View_OpenStatisticsClicked(object? sender, EventArgs e)
        {
            OnOpenStatistics?.Invoke(this, EventArgs.Empty);
        }

        // 新增功能：错题本导出 - 导出事件处理
        private void View_ExportErrorBookClicked(object? sender, EventArgs e)
        {
            try
            {
                // 获取用户的学习进度
                var profile = _persistenceService.LoadUserProfile(_currentUserId);
                var allUnknownItems = new List<string>();

                // 收集所有分类的未掌握项目
                foreach (var catProgress in profile.LearningProgress.CategoryProgresses.Values)
                {
                    allUnknownItems.AddRange(catProgress.UnknownItems);
                }

                if (allUnknownItems.Count == 0)
                {
                    _view?.ShowMessage("错题本为空，没有可导出的内容！");
                    return;
                }

                // 显示保存文件对话框
                using var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "文本文件 (*.txt)|*.txt|CSV文件 (*.csv)|*.csv";
                saveFileDialog.Title = "保存错题本";
                saveFileDialog.FileName = $"错题本_{_currentUserId}_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 根据文件扩展名选择格式
                    var content = string.Join(Environment.NewLine, allUnknownItems.Distinct());
                    
                    File.WriteAllText(saveFileDialog.FileName, content, System.Text.Encoding.UTF8);
                    
                    _view?.ShowMessage($"错题本已成功导出到：\n{saveFileDialog.FileName}");
                    _logger.LogInformation($"错题本已导出到 {saveFileDialog.FileName}，共 {allUnknownItems.Distinct().Count()} 个项目");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出错题本失败");
                _view?.ShowMessage($"导出错题本失败：{ex.Message}");
            }
        }

        private void View_TabChanged(object? sender, EventArgs e)
        {
            SaveSession();
        }

        private void SaveSession()
        {
            var session = _persistenceService.LoadSession();
            session.CurrentUserId = _currentUserId;
            session.LastAccessTime = DateTime.Now;
            _persistenceService.SaveSession(session);
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
            _logger.LogInformation("MainPresenter disposed");
        }
    }

    public class LearningStartEventArgs : EventArgs
    {
        public string UserId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string WordBankFile { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string SortOrder { get; set; } = string.Empty;
        public bool ContinueMode { get; set; }
    }
}
