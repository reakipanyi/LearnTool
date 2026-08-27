using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms;
using LearningAssistant.Forms.Games;
using LearningAssistant.Forms.Learning;
using LearningAssistant.Forms.Main;
using LearningAssistant.Forms.Pdf;
using LearningAssistant.Forms.Web;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Web;
using LearningAssistant.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    public class WindowManager : IWindowManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WindowManager> _logger;

        // 单实例聚焦：同类型窗口只保留一个，重复打开时聚焦已有窗口
        private readonly Dictionary<Type, Form> _openForms = new();

        public event EventHandler? SettingUsersChanged;

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

                    // 5. 显示窗口（非模态，允许多窗口并存）
                    ShowNonModalScopedForm(scope, form);
                }
                else
                {
                    _logger.LogError("ILearningView is not implemented as Form");
                    throw new InvalidOperationException("ILearningView 未实现为 Form 类型。");
                }
            }
            catch
            {
                // 创建/初始化失败时释放 scope；成功路径下 scope 由窗口关闭事件释放
                scope.Dispose();
                throw;
            }
        }

        public void OpenSettingsWindow()
        {
            _logger.LogInformation("Opening settings window");

            // 单例聚焦：已有设置窗口则提到前台
            if (TryFocusExisting(typeof(SettingForm))) return;

            var scope = _serviceProvider.CreateScope();
            try
            {
                var presenter = scope.ServiceProvider.GetRequiredService<SettingPresenter>();
                presenter.Initialize();

                var view = scope.ServiceProvider.GetRequiredService<ISettingView>();
                if (view is not Form form)
                {
                    _logger.LogError("ISettingView is not implemented as Form");
                    throw new InvalidOperationException("ISettingView 未实现为 Form 类型。");
                }

                // 桥接：设置窗体用户变更 → IWindowManager.SettingUsersChanged
                view.UsersChanged += (s, e) => SettingUsersChanged?.Invoke(this, EventArgs.Empty);

                form.StartPosition = FormStartPosition.CenterParent;
                ShowNonModalScopedForm(scope, form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open settings window");
                scope.Dispose();
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

            // 单例聚焦：已有编辑器窗口则提到前台
            if (TryFocusExisting(typeof(ContentEditorForm))) return;

            var scope = _serviceProvider.CreateScope();
            try
            {
                var scopedProvider = scope.ServiceProvider;

                // 获取 ContentEditorForm 实例
                var form = scopedProvider.GetRequiredService<ContentEditorForm>();
                var presenter = scopedProvider.GetRequiredService<ContentEditorPresenter>();

                // 设置 Presenter
                form.SetPresenter(presenter);


                if (!string.IsNullOrEmpty(language))
                {
                    if (SubjectSubCategoryMapping.TryParseSubject(language, out var subject))
                    {
                        form.SetInitialSubject(subject);
                    }
                }

                if (!string.IsNullOrEmpty(subCategory))
                {
                    if (SubjectSubCategoryMapping.TryParseSubCategory(subCategory, out var subCat))
                    {
                        form.SetInitialSubCategory(subCat);
                    }
                }

                form.StartPosition = FormStartPosition.CenterParent;
                ShowNonModalScopedForm(scope, form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open editor window");
                scope.Dispose();
                throw;
            }
        }

        public void OpenWordMatchGameWindow()
        {
            _logger.LogInformation("Opening word match game window");
            ShowScopedForm<WordMatchGameForm>();
        }

        public void OpenMemoryMatchGameWindow()
        {
            _logger.LogInformation("Opening memory match game window");
            ShowScopedForm<MemoryMatchGameForm>();
        }

        public void OpenLinkMatchGameWindow()
        {
            _logger.LogInformation("Opening link match game window");
            ShowScopedForm<LinkMatchGameForm>();
        }

        public void OpenSpellingGameWindow()
        {
            _logger.LogInformation("Opening spelling game window");
            ShowScopedForm<SpellingGameForm>();
        }

        public void OpenWhackAMoleGameWindow()
        {
            _logger.LogInformation("Opening whack-a-mole game window");
            ShowScopedForm<WhackAMoleGameForm>();
        }

        public void OpenSchulteGameWindow()
        {
            _logger.LogInformation("Opening schulte grid game window");
            ShowScopedForm<SchulteGameForm>();
        }

        public void OpenSudokuGameWindow()
        {
            _logger.LogInformation("Opening sudoku game window");
            ShowScopedForm<SudokuGameForm>();
        }

        public void OpenStatisticsWindow()
        {
            // 04 数据中心：统计入口统一指向学习数据中心（LearningManagementForm），
            // 不再由 ResultForm 承担"统计"角色。
            _logger.LogInformation("Opening statistics window (learning data center)");
            OpenLearningManagementWindow();
        }

        public void OpenLearningManagementWindow()
        {
            _logger.LogInformation("Opening learning management window");
            ShowScopedForm<LearningManagementForm>();
        }

        public void OpenPdfReaderWindow()
        {
            _logger.LogInformation("Opening PDF reader window");

            // 单例聚焦：已有 PDF 阅读器窗口则提到前台
            if (TryFocusExisting(typeof(PdfReaderForm))) return;

            var scope = _serviceProvider.CreateScope();
            try
            {
                var scopedProvider = scope.ServiceProvider;

                var form = scopedProvider.GetRequiredService<PdfReaderForm>();
                var presenter = scopedProvider.GetRequiredService<PdfPresenter>();

                form.SetPresenter(presenter);
                form.StartPosition = FormStartPosition.CenterParent;
                ShowNonModalScopedForm(scope, form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF reader window");
                scope.Dispose();
                throw;
            }
        }

        //public void OpenPdfReaderWindowV1()
        //{
        //    _logger.LogInformation("Opening PDF reader window");
        //
        //    try
        //    {
        //        using var scope = _serviceProvider.CreateScope();
        //        var scopedProvider = scope.ServiceProvider;
        //
        //        var form = scopedProvider.GetRequiredService<PdfReaderForm>();
        //        var presenter = scopedProvider.GetRequiredService<PdfPresenter>();
        //
        //        form.SetPresenter(presenter);
        //        form.StartPosition = FormStartPosition.CenterParent;
        //        form.ShowDialog();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to open PDF reader window");
        //        throw;
        //    }
        //}
        public void OpenNotesWindow()
        {
            _logger.LogInformation("Opening notes window");

        }

        public void OpenAIWebViewWindow(string? initialPrompt = null)
        {
            _logger.LogInformation("Opening AI WebView window");

            // 单例聚焦：已有 AI 浏览器窗口则提到前台
            if (TryFocusExisting(typeof(WebView2BrowserForm))) return;

            try
            {
                var cloudStorageService = _serviceProvider.GetService<ICloudStorageService>();
                var logger = _serviceProvider.GetService<ILogger<WebView2BrowserForm>>();
                var webBookmarkService = _serviceProvider.GetService<IWebBookmarkService>();
                var themeService = _serviceProvider.GetService<IThemeService>();
                var analysisOrchestrator = _serviceProvider.GetService<Services.PanAnalysis.IBaiduPanAnalysisOrchestrator>();
                var panAnalysisLogger = _serviceProvider.GetService<ILogger<BaiduPanAnalysisForm>>();
                var aiPanelPopupService = _serviceProvider.GetService<IAIPanelPopupService>();
                var aiConfig = _serviceProvider.GetService<AiConfig>();
                var panPromptBuilder = _serviceProvider.GetService<Services.PanAnalysis.IPanAnalysisPromptBuilder>();

                var form = new WebView2BrowserForm(cloudStorageService, logger, webBookmarkService, themeService,
                    analysisOrchestrator: analysisOrchestrator,
                    panAnalysisLogger: panAnalysisLogger,
                    aiPanelPopupService: aiPanelPopupService,
                    aiConfig: aiConfig,
                    panPromptBuilder: panPromptBuilder);
                form.InitialPrompt = initialPrompt;
                form.StartPosition = FormStartPosition.CenterParent;
                ShowNonModalForm(form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open AI WebView window");
                throw;
            }
        }

        #region 非模态窗口辅助方法

        /// <summary>
        /// 通用：从 DI scope 创建并显示一个窗口，把 scope 生命周期绑定到窗口关闭事件。
        /// </summary>
        private void ShowScopedForm<TForm>() where TForm : Form
        {
            if (TryFocusExisting(typeof(TForm))) return;

            var scope = _serviceProvider.CreateScope();
            TForm? form = null;
            try
            {
                form = scope.ServiceProvider.GetRequiredService<TForm>();
                form.StartPosition = FormStartPosition.CenterParent;
                ShowNonModalScopedForm(scope, form);
            }
            catch
            {
                form?.Dispose();
                scope.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 显示一个由 scope 提供依赖的窗口（非模态），scope 在窗口关闭时释放。
        /// 调用前需自行初始化 form 的 Presenter 等依赖，并把 scope 引用传入。
        /// </summary>
        private void ShowNonModalScopedForm(IServiceScope scope, Form form)
        {
            var type = form.GetType();
            // 清理已被释放的旧占位
            if (_openForms.TryGetValue(type, out var stale) && (stale == null || stale.IsDisposed))
                _openForms.Remove(type);

            // 注册并绑定关闭清理逻辑
            _openForms[type] = form;
            form.FormClosed += (s, e) =>
            {
                if (_openForms.TryGetValue(type, out var current) && ReferenceEquals(current, form))
                    _openForms.Remove(type);
                scope.Dispose();
            };

            // 非模态显示，允许多窗口并存
            form.Show();
        }

        /// <summary>
        /// 显示一个非 scope 来源的窗口（直接 new 出来的），无需 scope 释放。
        /// </summary>
        private void ShowNonModalForm(Form form)
        {
            var type = form.GetType();
            if (_openForms.TryGetValue(type, out var stale) && (stale == null || stale.IsDisposed))
                _openForms.Remove(type);

            _openForms[type] = form;
            form.FormClosed += (s, e) =>
            {
                if (_openForms.TryGetValue(type, out var current) && ReferenceEquals(current, form))
                    _openForms.Remove(type);
            };
            form.Show();
        }

        /// <summary>
        /// 若指定类型窗口已存在且未关闭，提到前台并返回 true；否则返回 false。
        /// </summary>
        private bool TryFocusExisting(Type type)
        {
            if (!_openForms.TryGetValue(type, out var existing))
                return false;

            if (existing == null || existing.IsDisposed)
            {
                _openForms.Remove(type);
                return false;
            }

            if (existing.WindowState == FormWindowState.Minimized)
                existing.WindowState = FormWindowState.Normal;
            existing.Show();
            existing.Activate();
            existing.BringToFront();
            return true;
        }

        #endregion
    }
}
