using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace LearningAssistant.Presenters
{
    public class SettingPresenter : IDisposable
    {
        private readonly ILogger<SettingPresenter> _logger;
        private readonly ISettingView _view;
        private readonly IDataPersistenceService _persistenceService;
        private readonly AppConfig _appConfig;
        private readonly IServiceProvider _serviceProvider;
        private string? _currentUserId;

        public SettingPresenter(ILogger<SettingPresenter> logger, ISettingView view, IDataPersistenceService persistenceService, AppConfig appConfig, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _view.SaveClicked += View_SaveClicked;
            _view.CancelClicked += View_CancelClicked;
            _view.AddUserClicked += View_AddUserClicked;
            _view.DeleteUserClicked += View_DeleteUserClicked;
            _logger.LogInformation("SettingPresenter initialized");
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing SettingPresenter");
            LoadConfigToView();
            LoadUserList();
        }

        private void LoadUserList()
        {
            try
            {
                var users = _persistenceService.GetUserIds();
                _view.SetUserList(users);

                // 保持当前登录用户被选中
                var session = _persistenceService.LoadSession();
                _currentUserId = session.CurrentUserId;
                if (!string.IsNullOrEmpty(_currentUserId))
                    _view.SelectedUserId = _currentUserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user list");
            }
        }

        private void LoadConfigToView()
        {
            var provider = string.IsNullOrWhiteSpace(_appConfig.TtsConfig.Provider) ? TtsProviders.KokoroSharp : _appConfig.TtsConfig.Provider;
            _view.TtsProvider = provider;
            _view.TTSEnabled = provider.Equals(TtsProviders.KokoroSharp, StringComparison.OrdinalIgnoreCase) ||
                               !string.IsNullOrWhiteSpace(_appConfig.TtsConfig.ApiKey);
            _view.TtsApiKey = _appConfig.TtsConfig.ApiKey;
            _view.TtsVoice = _appConfig.TtsConfig.Voice;
            _view.TTSSpeed = (int)(_appConfig.TtsConfig.Speed * 100);
            _view.TTSVolume = (int)(_appConfig.TtsConfig.Volume * 100);
            _view.FontSize = _appConfig.AppSettings.DefaultFontSize;
            _view.Theme = _appConfig.AppSettings.Theme;
            _view.BaiduAppId = _appConfig.TranslationConfig.BaiduAppId;
            _view.BaiduSecret = _appConfig.TranslationConfig.BaiduSecret;
            _view.IsVoiceEnabled = _appConfig.AppSettings.IsVoiceEnabled;
            _view.PronunciationScope = _appConfig.AppSettings.PronunciationScope;
            _view.IsAIExplanationEnabled = _appConfig.AppSettings.IsAIExplanationEnabled;
        }

        private void View_SaveClicked(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Saving settings");

                string oldVoice = _appConfig.TtsConfig.Voice;
                string oldProvider = _appConfig.TtsConfig.Provider;

                _appConfig.TtsConfig.Provider = _view.TtsProvider;
                _appConfig.TtsConfig.ApiKey = _view.TtsApiKey;
                _appConfig.TtsConfig.Voice = _view.TtsVoice;
                _appConfig.TtsConfig.Speed = _view.TTSSpeed / 100f;
                _appConfig.TtsConfig.Volume = _view.TTSVolume / 100f;
                _appConfig.AppSettings.DefaultFontSize = _view.FontSize;
                _appConfig.AppSettings.Theme = _view.Theme;
                _appConfig.TranslationConfig.BaiduAppId = _view.BaiduAppId;
                _appConfig.TranslationConfig.BaiduSecret = _view.BaiduSecret;
                _appConfig.AppSettings.IsVoiceEnabled = _view.IsVoiceEnabled;
                _appConfig.AppSettings.PronunciationScope = _view.PronunciationScope;
                _appConfig.AppSettings.IsAIExplanationEnabled = _view.IsAIExplanationEnabled;

                _persistenceService.SaveConfig(_appConfig);
                _persistenceService.PersistCache();

                if (!string.Equals(oldVoice, _appConfig.TtsConfig.Voice, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(oldProvider, _appConfig.TtsConfig.Provider, StringComparison.OrdinalIgnoreCase))
                {
                    NotifyTtsServiceSettingsChanged();
                }

                //_view.ShowMessage("设置已保存");
                _view.CloseView();
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                _view.ShowMessage($"保存失败: {ex.Message}");
            }
        }

        private void NotifyTtsServiceSettingsChanged()
        {
            try
            {
                var ttsService = _serviceProvider.GetService<ITTSService>();
                if (ttsService is KokoroSharpTtsService kokoroService)
                {
                    kokoroService.ReloadVoiceSettings();
                    _logger.LogInformation("Notified KokoroSharp TTS service to reload voice settings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify TTS service of settings change");
            }
        }

        private void View_CancelClicked(object? sender, EventArgs e)
        {
            _view.CloseView();
        }

        private void View_AddUserClicked(object? sender, EventArgs e)
        {
            try
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox("请输入新用户名称:", "添加用户", "");
                if (string.IsNullOrWhiteSpace(input))
                    return;

                var userId = input.Trim();
                var existing = _persistenceService.GetUserIds();
                if (existing.Contains(userId))
                {
                    _view.ShowMessage($"用户 \"{userId}\" 已存在，请使用其他名称。");
                    return;
                }

                _persistenceService.CreateUserProfile(userId, userId);
                AppPaths.EnsureUserDirectoriesExist(userId);
                _logger.LogInformation("User created: {UserId}", userId);
                _view.ShowMessage($"用户 \"{userId}\" 创建成功。");
                LoadUserList();
                _view.SelectedUserId = userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user");
                _view.ShowMessage($"添加用户失败：{ex.Message}");
            }
        }

        private void View_DeleteUserClicked(object? sender, EventArgs e)
        {
            try
            {
                var selected = _view.SelectedUserId;
                if (string.IsNullOrWhiteSpace(selected))
                {
                    _view.ShowMessage("请先在列表中选择要删除的用户。");
                    return;
                }

                // 禁止删除最后一个用户
                var users = _persistenceService.GetUserIds();
                if (users.Count <= 1)
                {
                    _view.ShowMessage("至少需要保留一个用户，无法删除。");
                    return;
                }

                // 禁止删除当前登录用户
                if (string.Equals(selected, _currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    _view.ShowMessage("无法删除当前正在使用的用户，请先切换到其他用户。");
                    return;
                }

                var confirm = MessageBox.Show(
                    $"确定要删除用户 \"{selected}\" 吗？\n\n该操作将清除该用户的所有学习数据（进度、收藏、错题、笔记等），且不可恢复！",
                    "确认删除用户",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                    return;

                var deleted = _persistenceService.DeleteUserProfile(selected);
                if (deleted)
                {
                    _logger.LogInformation("User deleted: {UserId}", selected);
                    _view.ShowMessage($"用户 \"{selected}\" 已删除。");
                    LoadUserList();
                }
                else
                {
                    _view.ShowMessage($"未找到用户 \"{selected}\"，可能已被删除。");
                    LoadUserList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user");
                _view.ShowMessage($"删除用户失败：{ex.Message}");
            }
        }

        public void Dispose()
        {
            _view.SaveClicked -= View_SaveClicked;
            _view.CancelClicked -= View_CancelClicked;
            _view.AddUserClicked -= View_AddUserClicked;
            _view.DeleteUserClicked -= View_DeleteUserClicked;
            _logger.LogInformation("SettingPresenter disposed");
        }
    }
}