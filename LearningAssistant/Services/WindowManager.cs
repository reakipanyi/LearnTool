using LearningAssistant.Forms;
using LearningAssistant.Presenters;
using LearningAssistant.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services
{
    public interface IWindowManager
    {
        Task OpenLearningWindowAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode);
        void OpenSettingsWindow();
        void OpenEditorWindow();
        void OpenEditorWindowWithContext(string? text, string? language, string? subCategory);
        void OpenStatisticsWindow();
        void OpenLearningManagementWindow();
        void OpenPdfReaderWindow();
        void OpenAIWebViewWindow(string? initialPrompt = null);
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

        public async Task OpenLearningWindowAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode)
        {
            _logger.LogInformation("Opening learning window for user {UserId}, category {SubCategory}, continueMode={ContinueMode}", userId, subCategory, continueMode);

            // 创建 scope 并保留引用，避免过早释放
            var scope = _serviceProvider.CreateScope();
            try
            {
                var presenter = scope.ServiceProvider.GetRequiredService<LearningPresenter>();
                var view = scope.ServiceProvider.GetRequiredService<ILearningView>();

                if (view is Form form)
                {
                    // 1. 先设置初始状态
                    view.SetLoadingState(true, "正在启动...");

                    // 2. 配置窗口位置
                    form.StartPosition = FormStartPosition.CenterParent;

                    // 3. 注册 Shown 事件，在窗口显示后异步加载内容
                    form.Shown += async (sender, args) =>
                    {
                        _logger.LogInformation("Learning window shown, starting async initialization");
                        try
                        {
                            // 4. 异步初始化学习内容
                            await presenter.InitializeAsync(userId, language, subCategory, wordBankFile, continueMode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to initialize learning window");
                            view.ShowMessage($"初始化失败：{ex.Message}");
                        }
                    };

                    // 5. 显示窗口（不阻塞）
                    form.ShowDialog();
                }
                else
                {
                    _logger.LogError("ILearningView is not implemented as Form");
                    throw new InvalidOperationException("ILearningView 未实现为 Form 类型。");
                }
            }
            finally
            {
                // 在对话框关闭后释放 scope
                scope.Dispose();
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

        public void OpenLearningManagementWindow()
        {
            _logger.LogInformation("Opening learning management window");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var form = scope.ServiceProvider.GetRequiredService<LearningManagementForm>();
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open learning management window");
                throw;
            }
        }

        public void OpenPdfReaderWindow()
        {
            _logger.LogInformation("Opening PDF reader window");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;

                var form = scopedProvider.GetRequiredService<PdfReaderForm>();
                var presenter = scopedProvider.GetRequiredService<PdfPresenter>();

                form.SetPresenter(presenter);
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF reader window");
                throw;
            }
        }

        public void OpenAIWebViewWindow(string? initialPrompt = null)
        {
            _logger.LogInformation("Opening AI WebView window");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;

                var form = scopedProvider.GetRequiredService<AIWebViewForm>();
                form.InitialPrompt = initialPrompt;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open AI WebView window");
                throw;
            }
        }
    }
}
