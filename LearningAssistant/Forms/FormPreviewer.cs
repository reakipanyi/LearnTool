using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 窗体预览工具 - 用于预览所有窗体的样式
    /// </summary>
    public static class FormPreviewer
    {
        /// <summary>
        /// 创建并预览指定的窗体
        /// </summary>
        public static Form CreatePreviewForm(string formName)
        {
            return formName.ToLower() switch
            {
                "mainform" => CreateMainFormPreview(),
                "learningform" => CreateLearningFormPreview(),
                "contenteditorform" => CreateContentEditorFormPreview(),
                "pdfreaderform" => CreatePdfReaderFormPreview(),
                "resultform" => CreateResultFormPreview(),
                "settingform" => CreateSettingFormPreview(),
                "achievementnotificationform" => CreateAchievementNotificationFormPreview(),
                _ => throw new ArgumentException($"未知的窗体名称: {formName}")
            };
        }

        /// <summary>
        /// 获取所有可预览的窗体列表
        /// </summary>
        public static string[] GetPreviewableForms()
        {
            return new[]
            {
                "MainForm",
                "LearningForm",
                "ContentEditorForm",
                "PdfReaderForm",
                "ResultForm",
                "SettingForm",
                "AchievementNotificationForm"
            };
        }

        #region 窗体预览创建方法

        private static Form CreateMainFormPreview()
        {
            var mockPresenter = CreateMockMainPresenter();
            var mockWindowManager = CreateMockWindowManager();
            var mockAppConfig = new Models.Config.AppConfig();
            var mockCloudStorageService = CreateMockCloudStorageService();
            var mockThemeService = CreateMockThemeService();

            return new MainForm(mockPresenter, mockWindowManager, mockAppConfig, mockCloudStorageService, mockThemeService);
        }

        private static Form CreateLearningFormPreview()
        {
            var logger = NullLoggerFactory.Instance.CreateLogger<LearningForm>();
            var aiQuestionService = CreateMockAiQuestionService();
            var ttsService = CreateMockTtsService();
            var aiPanelPopupService = CreateMockAIPanelPopupService();
            var themeService = CreateMockThemeService();

            return new LearningForm(aiQuestionService, ttsService, logger, NullLoggerFactory.Instance, ttsService, themeService, aiPanelPopupService);
        }

        private static Form CreateContentEditorFormPreview()
        {
            var logger = NullLoggerFactory.Instance.CreateLogger<ContentEditorForm>();
            var appConfig = new Models.Config.AppConfig();
            var aiPanelPopupService = CreateMockAIPanelPopupService();
            var themeService = CreateMockThemeService();

            return new ContentEditorForm(logger, appConfig, aiPanelPopupService, themeService);
        }

        private static Form CreatePdfReaderFormPreview()
        {
            var logger = NullLoggerFactory.Instance.CreateLogger<PdfReaderForm>();
            var ttsService = CreateMockTtsService();

            return new PdfReaderForm(logger, ttsService);
        }

        private static Form CreateResultFormPreview()
        {
            var themeService = CreateMockThemeService();
            return new ResultForm(NullLogger<ResultForm>.Instance, themeService);
        }

        private static Form CreateSettingFormPreview()
        {
            var themeService = CreateMockThemeService();
            var logger = NullLoggerFactory.Instance.CreateLogger<SettingForm>();
            return new SettingForm(logger, themeService);
        }

        private static Form CreateAchievementNotificationFormPreview()
        {
            var achievement = new Models.User.Achievement
            {
                Id = "preview",
                Name = "学习达人",
                Description = "连续学习7天",
                Icon = "🏆"
            };
            var soundService = new SoundService();

            return new AchievementNotificationForm(achievement, soundService);
        }

        #endregion

        #region Mock 服务创建

        private static MainPresenter CreateMockMainPresenter()
        {
            var userProfileService = CreateMockUserProfileService();
            var contentLoaderService = CreateMockContentLoaderService();
            var achievementService = CreateMockAchievementService();
            var windowManager = CreateMockWindowManager();
            var logger = NullLoggerFactory.Instance.CreateLogger<MainPresenter>();

            return new MainPresenter(userProfileService, contentLoaderService, achievementService, windowManager, logger);
        }

        private static IUserProfileService CreateMockUserProfileService()
        {
            var mockService = new MockUserProfileService();
            return mockService;
        }

        private static IContentLoaderService CreateMockContentLoaderService()
        {
            var mockService = new MockContentLoaderService();
            return mockService;
        }

        private static IAchievementService CreateMockAchievementService()
        {
            var mockService = new MockAchievementService();
            return mockService;
        }

        private static IWindowManager CreateMockWindowManager()
        {
            var mockService = new MockWindowManager();
            return mockService;
        }

        private static IPdfView CreateMockPdfView()
        {
            return new MockPdfView();
        }

        private static IAiQuestionService CreateMockAiQuestionService()
        {
            return new MockAiQuestionService();
        }

        private static ITTSService CreateMockTtsService()
        {
            return new MockTtsService();
        }


        private static IAIPanelPopupService CreateMockAIPanelPopupService()
        {
            return new MockAIPanelPopupService();
        }

        private static ICloudStorageService CreateMockCloudStorageService()
        {
            return new MockCloudStorageService();
        }

        private static IThemeService CreateMockThemeService()
        {
            var mockEventBus = new MockEventBus();
            return new Common.ThemeService(mockEventBus);
        }

        #endregion
    }

    #region Mock 服务实现

    internal class MockUserProfileService : IUserProfileService
    {
        public Task<Models.User.UserProfile> GetProfileAsync(string userId) => Task.FromResult(new Models.User.UserProfile());
        public Task SaveProfileAsync(Models.User.UserProfile profile) => Task.CompletedTask;
        public Task<List<Models.User.UserProfile>> GetAllProfilesAsync() => Task.FromResult(new List<Models.User.UserProfile>());
        public Task UpdateLearningSessionAsync(string userId, Models.User.LearningSession session) => Task.CompletedTask;
    }

    internal class MockContentLoaderService : IContentLoaderService
    {
        public Task<List<Models.Learning.LearningItem>> LoadLearningItemsAsync(string filePath) => Task.FromResult(new List<Models.Learning.LearningItem>());
        public Task SaveLearningItemsAsync(string filePath, List<Models.Learning.LearningItem> items) => Task.CompletedTask;
        public string[] GetAvailableWordBanks(string language) => new string[0];
        public string[] GetAvailableSubCategories(string language) => new string[0];
    }

    internal class MockAchievementService : IAchievementService
    {
        public Task<List<Models.User.Achievement>> GetAchievementsAsync(string userId) => Task.FromResult(new List<Models.User.Achievement>());
        public Task CheckAndUnlockAchievementsAsync(string userId, Models.User.UserProfile profile) => Task.CompletedTask;
        public event EventHandler<Models.User.AchievementUnlockedEventArgs>? AchievementUnlocked;
    }

    internal class MockWindowManager : IWindowManager
    {
        public Task OpenLearningWindowAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode) => Task.CompletedTask;
        public void OpenSettingsWindow() { }
        public void OpenEditorWindow() { }
        public void OpenEditorWindowWithContext(string text, string language, string? subCategory) { }
        public void OpenStatisticsWindow() { }
    }

    internal class MockPdfView : UserControl, IPdfView
    {
        public event EventHandler? AddToEditorRequested;
        public event EventHandler<AddToEditorEventArgs>? OnAddToEditor;
        public string SelectedText => "";
        public string CurrentLanguage => "中文";
        public void SetPresenter(PdfPresenter presenter) { }
        public void UpdateSelectedText(string text) { }
    }

    internal class MockAiQuestionService : IAiQuestionService
    {
        public Task<string> AskQuestionAsync(string content, string question) => Task.FromResult("");
    }

    internal class MockTtsService : ITTSService
    {
        public Task SpeakAsync(string text, string language) => Task.CompletedTask;
        public void Stop() { }
    }

    internal class MockAIPanelPopupService : IAIPanelPopupService
    {
        public void ShowAIAbilityPanel(Form parent, string? prompt = null, string? aiUrl = null) { }
        public void HideAIAbilityPanel(Form parent) { }
    }

    internal class MockCloudStorageService : ICloudStorageService
    {
        public Task<bool> AuthenticateAsync() => Task.FromResult(false);
        public Task<List<string>> ListFilesAsync(string path) => Task.FromResult(new List<string>());
        public Task DownloadFileAsync(string remotePath, string localPath) => Task.CompletedTask;
        public Task UploadFileAsync(string localPath, string remotePath) => Task.CompletedTask;
        public Task DeleteFileAsync(string remotePath) => Task.CompletedTask;
        public Task<string> GetShareUrlAsync(string remotePath) => Task.FromResult("");
        public bool IsAuthenticated => false;
    }

    internal class MockEventBus : IEventBus
    {
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : Common.Events.IApplicationEvent { }
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : Common.Events.IApplicationEvent { }
        public void Publish<TEvent>(TEvent @event) where TEvent : Common.Events.IApplicationEvent { }
    }

    #endregion
}
