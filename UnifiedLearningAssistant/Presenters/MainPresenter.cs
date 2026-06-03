using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Services;
using UnifiedLearningAssistant.Services.Cache;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Services.Persistence;
using UnifiedLearningAssistant.Views;
using UnifiedLearningAssistant.Models.User;

namespace UnifiedLearningAssistant.Presenters
{
    public class MainPresenter : IDisposable
    {
        private readonly ILogger<MainPresenter> _logger;
        private readonly IUserSessionService _sessionService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IProgressService _progressService;
        private readonly IExportService _exportService;
        private readonly ITTSService _ttsService;
        private readonly ICacheService _cacheService;
        private readonly IWindowManager _windowManager;
        private readonly IDataPersistenceService _persistenceService;

        private PdfPresenter? _pdfPresenter;
        private IMainView? _view;

        private string _currentUserId = "Guest";
        private string _currentLanguage = Constants.Language.Chinese;
        private string _currentSubCategory = Constants.SubCategory.ChineseCharacter;
        private string _currentMode = Constants.LearningMode.Study;
        private string _currentWordBankFile = string.Empty;
        private string _currentSortOrder = Constants.SortOrder.Sequential;
        private UserProfile? _currentUserProfile;

        public event EventHandler<LearningStartEventArgs>? OnStartLearning;
        public event EventHandler? OnOpenSettings;
        public event EventHandler? OnOpenEditor;
        public event EventHandler? OnOpenStatistics;

        public MainPresenter(
            ILogger<MainPresenter> logger,
            IUserSessionService sessionService,
            IContentLoaderService contentLoaderService,
            IProgressService progressService,
            IExportService exportService,
            ITTSService ttsService,
            ICacheService cacheService,
            IWindowManager windowManager,
            IDataPersistenceService persistenceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _logger.LogInformation("MainPresenter initialized");
        }

        public void SetPdfPresenter(PdfPresenter pdfPresenter)
        {
            _pdfPresenter = pdfPresenter;
            UpdatePdfPresenterConfig();
        }

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
            _view.ExportErrorBookClicked -= View_ExportErrorBookClicked;
            _view.TabChanged -= View_TabChanged;
        }

        public void Initialize()
        {
            if (_view == null)
                return;

            _logger.LogInformation("Initializing MainPresenter");
            LoadSession();
            LoadLearningConfig();
            RefreshUserList();
            RefreshSubCategories();
            RefreshWordBankFiles();
            UpdateProgressSummary();
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
                _currentUserProfile = _persistenceService.LoadUserProfile(_currentUserId);
                if (_currentUserProfile != null)
                {
                    _currentUserProfile.ResetDailyStats();
                    UpdateStreakInfo();
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
            }
        }

        private void LoadLearningConfig()
        {
            var config = _sessionService.LoadLearningConfig();
            if (!string.IsNullOrWhiteSpace(config.Language))
            {
                _currentLanguage = config.Language;
                _view.SelectedLanguage = _currentLanguage;
            }
            if (!string.IsNullOrWhiteSpace(config.Mode))
            {
                _currentMode = config.Mode;
                _view.SelectedMode = _currentMode;
            }
            if (!string.IsNullOrWhiteSpace(config.SortOrder))
            {
                _currentSortOrder = config.SortOrder;
                _view.SelectedSortOrder = _currentSortOrder;
            }
            if (!string.IsNullOrWhiteSpace(config.SubCategory))
            {
                _currentSubCategory = config.SubCategory;
            }
            if (!string.IsNullOrWhiteSpace(config.WordBankFile))
            {
                _currentWordBankFile = config.WordBankFile;
            }
        }

        private void RefreshUserList()
        {
            var users = _sessionService.GetUserList();
            _view.RefreshUserList(users);
        }

        private void RefreshSubCategories()
        {
            var subCats = _contentLoaderService.GetSubCategories(_currentLanguage);
            _view.RefreshSubCategories(subCats);
            if (subCats.Any())
            {
                if (!string.IsNullOrWhiteSpace(_currentSubCategory) && subCats.Contains(_currentSubCategory))
                {
                    _view.SelectedSubCategory = _currentSubCategory;
                }
                else
                {
                    _currentSubCategory = subCats.First();
                    _view.SelectedSubCategory = _currentSubCategory;
                }
                RefreshWordBankFiles();
                UpdateProgressSummary();
                UpdatePdfPresenterConfig();
            }
        }

        private void RefreshWordBankFiles()
        {
            var files = _contentLoaderService.GetWordBankFiles(_currentSubCategory);
            _view.RefreshWordBankFiles(files);
            if (files.Any())
            {
                if (!string.IsNullOrWhiteSpace(_currentWordBankFile) && files.Contains(_currentWordBankFile))
                {
                    _view.SelectedWordBankFile = _currentWordBankFile;
                }
                else
                {
                    var defaultFile = _contentLoaderService.GetDefaultWordBankFile(_currentSubCategory);
                    _currentWordBankFile = !string.IsNullOrWhiteSpace(defaultFile) ? defaultFile : files.First();
                    _view.SelectedWordBankFile = _currentWordBankFile;
                }
            }
        }

        private void UpdateProgressSummary()
        {
            _view.ProgressSummary = _progressService.GetProgressSummary(_currentUserId, _currentLanguage, _currentSubCategory);
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
            SaveLearningConfig();
        }

        private void View_SubCategoryChanged(object? sender, EventArgs e)
        {
            _currentSubCategory = _view.SelectedSubCategory;
            RefreshWordBankFiles();
            UpdateProgressSummary();
            UpdatePdfPresenterConfig();
            SaveLearningConfig();
        }

        private void View_ModeChanged(object? sender, EventArgs e)
        {
            _currentMode = _view.SelectedMode;
            SaveLearningConfig();
        }

        private void View_WordBankChanged(object? sender, EventArgs e)
        {
            _currentWordBankFile = _view.SelectedWordBankFile;
            SaveLearningConfig();
        }

        private void View_SortOrderChanged(object? sender, EventArgs e)
        {
            _currentSortOrder = _view.SelectedSortOrder;
            SaveLearningConfig();
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

        private void View_ExportErrorBookClicked(object? sender, EventArgs e)
        {
            try
            {
                var errorBookItems = _exportService.GetErrorBookItems(_currentUserId);
                
                if (errorBookItems.Count == 0)
                {
                    _view?.ShowMessage("错题本为空，没有可导出的内容！");
                    return;
                }

                using var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "文本文件 (*.txt)|*.txt|CSV文件 (*.csv)|*.csv";
                saveFileDialog.Title = "保存错题本";
                saveFileDialog.FileName = $"错题本_{_currentUserId}_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var result = _exportService.ExportErrorBook(_currentUserId, saveFileDialog.FileName);
                    _view?.ShowMessage(result);
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
            _sessionService.SaveSession(_currentUserId);
        }

        private void SaveLearningConfig()
        {
            _sessionService.SaveLearningConfig(_currentLanguage, _currentSubCategory, _currentMode, _currentWordBankFile, _currentSortOrder);
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