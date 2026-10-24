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
using LearningAssistant.Services.Learning;
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

        // 窗口注册表：允许多个同类型窗口并存（PDF/浏览器多开），
        // 需要单实例的窗口打开前用 TryFocusExisting 聚焦已有窗口。
        private readonly List<Form> _openForms = new();
        private readonly object _openFormsLock = new();

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

        public void OpenStroopGameWindow()
        {
            _logger.LogInformation("Opening stroop game window");
            ShowScopedForm<StroopGameForm>();
        }

        public void OpenInhibitionGameWindow()
        {
            _logger.LogInformation("Opening inhibition game window");
            ShowScopedForm<InhibitionGameForm>();
        }

        public void OpenMemoryGameWindow()
        {
            _logger.LogInformation("Opening memory game window");
            ShowScopedForm<MemoryGameForm>();
        }

        public void OpenSearchGameWindow()
        {
            _logger.LogInformation("Opening search game window");
            ShowScopedForm<SearchGameForm>();
        }

        public void OpenTraceGameWindow()
        {
            _logger.LogInformation("Opening trace game window");
            ShowScopedForm<TraceGameForm>();
        }

        public void OpenFlexGameWindow()
        {
            _logger.LogInformation("Opening flex game window");
            ShowScopedForm<FlexGameForm>();
        }

        public void OpenAttentionGameWindow()
        {
            _logger.LogInformation("Opening attention game window");
            ShowScopedForm<AttentionGameForm>();
        }

        public void OpenPlanGameWindow()
        {
            _logger.LogInformation("Opening plan game window");
            ShowScopedForm<PlanGameForm>();
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

        public void OpenPdfReaderWindow(string? pdfPath = null, int pageIndex = -1)
        {
            _logger.LogInformation("Opening PDF reader window");

            // 允许多开：每次打开都创建独立 scope，渲染服务互不干扰
            var scope = _serviceProvider.CreateScope();
            try
            {
                var scopedProvider = scope.ServiceProvider;

                var form = scopedProvider.GetRequiredService<PdfReaderForm>();
                var presenter = scopedProvider.GetRequiredService<PdfPresenter>();

                form.SetPresenter(presenter);
                // 多开时错开摆放：首个窗口最大化，后续窗口级联，避免所有窗口都最大化互相完全遮挡，
                // 导致之前的窗口既拖不动也点不到
                PlaceReaderWindow(form, GetWindows<PdfReaderForm>());
                // 从学习项回跳：窗口显示后加载指定 PDF 并定位到来源页
                if (!string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath))
                {
                    var targetPath = pdfPath;
                    var targetPage = pageIndex;
                    form.Shown += async (_, _) => await LoadPdfAtPageAsync(presenter, targetPath, targetPage);
                }
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
        /// <summary>
        /// 加载指定 PDF 并定位到来源页（学习项回跳原文）。
        /// </summary>
        private async Task LoadPdfAtPageAsync(PdfPresenter presenter, string pdfPath, int pageIndex)
        {
            try
            {
                await presenter.LoadPdf(pdfPath);
                if (pageIndex >= 0)
                    await presenter.RenderPage(pageIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF at page {PageIndex} from learning source: {PdfPath}", pageIndex, pdfPath);
            }
        }

        public void OpenNotesWindow()
        {
            _logger.LogInformation("Opening notes window");

            try
            {
                var noteService = _serviceProvider.GetService<INoteService>();
                if (noteService == null)
                {
                    MessageBox.Show("笔记服务不可用，无法打开笔记管理。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var sessionService = _serviceProvider.GetService<IUserSessionService>();
                var form = new NotesManagerForm(noteService, sessionService)
                {
                    StartPosition = FormStartPosition.CenterParent
                };
                ShowNonModalForm(form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open notes window");
                throw;
            }
        }

        public void OpenAIWebViewWindow(string? initialPrompt = null)
        {
            _logger.LogInformation("Opening AI WebView window");

            // 允许多开：每个浏览器窗口独立运行，不再聚焦已有窗口
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
            RegisterForm(form, onClosed: () => scope.Dispose());
            // 非模态显示，允许多窗口并存
            form.Show();
        }

        /// <summary>
        /// 安排 PDF 阅读器窗口的位置：首个窗口最大化，之后再开的新窗口在工作区内级联错开，
        /// 保证每个窗口都露出可拖动的标题栏，也能随时切换回去。
        /// </summary>
        private static void PlaceReaderWindow(Form form, IReadOnlyList<PdfReaderForm> existing)
        {
            if (existing.Count == 0)
            {
                form.WindowState = FormWindowState.Maximized;
                return;
            }

            // 以当前活跃窗口所在屏幕为基准，避免多显示器时错开到别的屏幕
            var reference = existing.FirstOrDefault(f => f.ContainsFocus)
                ?? existing.LastOrDefault(f => f.WindowState != FormWindowState.Minimized)
                ?? existing[^1];
            var area = Screen.FromControl(reference).WorkingArea;

            var size = new Size(
                Math.Max(form.MinimumSize.Width, area.Width * 2 / 3),
                Math.Max(form.MinimumSize.Height, area.Height * 3 / 4));

            // 最多累计 8 级偏移，避免开太多窗口后跑到屏幕外
            int offset = Math.Min(existing.Count, 8) * 32;
            int left = area.Left + (area.Width - size.Width) / 2 + offset;
            int top = area.Top + (area.Height - size.Height) / 2 + offset;
            if (left + size.Width > area.Right) left = area.Left + (area.Width - size.Width) / 2;
            if (top + size.Height > area.Bottom) top = area.Top + (area.Height - size.Height) / 2;

            // 必须先退出最大化再设 Bounds，否则位置与尺寸会被最大化状态覆盖
            form.WindowState = FormWindowState.Normal;
            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = new Rectangle(new Point(left, top), size);
        }

        /// <summary>
        /// 显示一个非 scope 来源的窗口（直接 new 出来的），无需 scope 释放。
        /// </summary>
        private void ShowNonModalForm(Form form)
        {
            RegisterForm(form, onClosed: null);
            form.Show();
        }

        /// <summary>注册窗口并在关闭时移除；同类型窗口允许多个实例并存。</summary>
        private void RegisterForm(Form form, Action? onClosed)
        {
            lock (_openFormsLock)
            {
                RemoveDisposedLocked();
                _openForms.Add(form);
            }

            _logger.LogDebug("窗口已注册：{WindowType}，当前窗口数 {Count}", form.GetType().Name, _openForms.Count);

            form.FormClosed += (s, e) =>
            {
                lock (_openFormsLock)
                {
                    _openForms.Remove(form);
                }
                _logger.LogDebug("窗口已关闭：{WindowType}", form.GetType().Name);
                onClosed?.Invoke();
            };
        }

        private void RemoveDisposedLocked() => _openForms.RemoveAll(f => f.IsDisposed);

        /// <summary>取指定类型的全部存活窗口（按打开顺序）。</summary>
        public IReadOnlyList<T> GetWindows<T>() where T : Form
        {
            lock (_openFormsLock)
            {
                RemoveDisposedLocked();
                return _openForms.OfType<T>().ToList();
            }
        }

        /// <summary>取指定类型的活跃窗口：优先返回当前拥有焦点的窗口，否则返回最后打开的窗口。</summary>
        public T? GetActive<T>() where T : Form
        {
            var windows = GetWindows<T>();
            return windows.FirstOrDefault(f => f.ContainsFocus) ?? windows.LastOrDefault();
        }

        /// <summary>
        /// 若指定类型窗口已存在且未关闭，提到前台并返回 true；否则返回 false。
        /// </summary>
        private bool TryFocusExisting(Type type)
        {
            Form? existing;
            lock (_openFormsLock)
            {
                RemoveDisposedLocked();
                existing = _openForms.LastOrDefault(f => type.IsInstanceOfType(f));
            }

            if (existing == null)
                return false;

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
