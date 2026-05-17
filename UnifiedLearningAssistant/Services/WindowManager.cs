using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Forms;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Services
{
    public interface IWindowManager
    {
        Task OpenLearningWindowAsync(string userId, string language, string subCategory, string wordBankFile, string mode, string sortOrder);
        void OpenSettingsWindow();
        void OpenEditorWindow();
        void OpenEditorWindowWithContext(string? text, string? language, string? subCategory);
        void OpenStatisticsWindow();
    }

    public class WindowManager : IWindowManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WindowManager> _logger;

        public WindowManager(IServiceProvider serviceProvider, ILogger<WindowManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OpenLearningWindowAsync(string userId, string language, string subCategory, string wordBankFile, string mode, string sortOrder)
        {
            _logger.LogInformation("Opening learning window for user {UserId}, category {SubCategory}", userId, subCategory);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var presenter = scope.ServiceProvider.GetRequiredService<LearningPresenter>();
                await presenter.InitializeAsync(userId, language, subCategory, wordBankFile, mode, sortOrder);

                var view = scope.ServiceProvider.GetRequiredService<ILearningView>();
                if (view is Form form)
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ShowDialog();
                }
                else
                {
                    _logger.LogError("ILearningView is not implemented as Form");
                    throw new InvalidOperationException("ILearningView 未实现为 Form 类型。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open learning window");
                throw;
            }
        }

        public void OpenSettingsWindow()
        {
            _logger.LogInformation("Opening settings window");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var presenter = scope.ServiceProvider.GetRequiredService<SettingPresenter>();
                presenter.Initialize();

                var view = scope.ServiceProvider.GetRequiredService<ISettingView>();
                if (view is Form form)
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ShowDialog();
                }
                else
                {
                    _logger.LogError("ISettingView is not implemented as Form");
                    throw new InvalidOperationException("ISettingView 未实现为 Form 类型。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open settings window");
                throw;
            }
        }

        public void OpenEditorWindow()
        {
            OpenEditorWindowWithContext(null, null, null);
        }

        public void OpenEditorWindowWithContext(string? text, string? language, string? subCategory)
        {
            _logger.LogInformation("Opening editor window with context");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;

                // 获取 ContentEditorForm 实例
                var form = scopedProvider.GetRequiredService<ContentEditorForm>();
                var presenter = scopedProvider.GetRequiredService<ContentEditorPresenter>();

                // 设置 Presenter
                form.SetPresenter(presenter);

                // 如果有上下文信息，设置到窗体
                if (!string.IsNullOrEmpty(text))
                {
                    form.GenerateRange = text;
                }

                if (!string.IsNullOrEmpty(language))
                {
                    // 设置语言选择
                    form.SetInitialLanguage(language);
                }

                if (!string.IsNullOrEmpty(subCategory))
                {
                    form.SetInitialSubCategory(subCategory);
                }

                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open editor window");
                throw;
            }
        }

        public void OpenStatisticsWindow()
        {
            _logger.LogInformation("Opening statistics window");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var presenter = scope.ServiceProvider.GetRequiredService<ResultPresenter>();
                presenter.Initialize();

                var view = scope.ServiceProvider.GetRequiredService<IResultView>();
                if (view is Form form)
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ShowDialog();
                }
                else
                {
                    _logger.LogError("IResultView is not implemented as Form");
                    throw new InvalidOperationException("IResultView 未实现为 Form 类型。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open statistics window");
                throw;
            }
        }
    }
}
