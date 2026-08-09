using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.Bookmark;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// WebView2 浏览器窗体，支持多标签页、多个 AI 平台快捷导航和百度网盘集成
    /// </summary>
    public partial class WebView2BrowserForm : Form, IThemeable
    {
        private readonly ICloudStorageService? _cloudStorageService;
        private readonly ILogger? _logger;
        private readonly IWebBookmarkService? _webBookmarkService;
        private readonly IThemeService? _themeService;
        private readonly IPendingContentService? _pendingContentService;
        private CoreWebView2Environment? _webViewEnvironment;
        private readonly Dictionary<TabPage, WebView2> _webViews = new();
        private string? _initialPrompt;
        private IReadOnlyList<WebBookmarkCategory>? _cachedBookmarkCategories;
        private List<ToolStripButton>? _providerButtons;
        private Dictionary<string, ToolStripButton>? _hostToButtonMapping;
        private Dictionary<int, string>? _bookmarkIndexToUrl;
        private ThemeMode _currentThemeMode = ThemeMode.Light;
        private int _tabCounter = 0;
        private int _zoomLevel = Config.DefaultZoomLevel;

        /// <summary>
        /// 当前活动的 WebView2
        /// </summary>
        private WebView2? CurrentWebView
        {
            get
            {
                if (tabControl?.SelectedTab is TabPage tab && _webViews.TryGetValue(tab, out var webView))
                    return webView;
                return null;
            }
        }

        /// <summary>
        /// URL 常量
        /// </summary>
        private static class Urls
        {
            public const string BaiduNetdisk = "https://pan.baidu.com";
            public const string Baidu = "https://www.baidu.com";
            public const string DeepSeek = "https://chat.deepseek.com/";
            public const string Doubao = "https://www.doubao.com/chat";
            public const string Zhipu = "https://chatglm.cn";
            public const string Qwen = "https://tongyi.aliyun.com/qianwen";
            public const string Spark = "https://xinghuo.xfyun.cn";
            public const string Wenxin = "https://yiyan.baidu.com";
        }

        /// <summary>
        /// 提示消息常量
        /// </summary>
        private static class Messages
        {
            public const string NetdiskNotConfigured = "百度网盘服务未配置或未授权";
            public const string NetdiskNotAuthorized = "请先完成百度网盘授权";
            public const string NetdiskNavigatePrompt = "请先浏览到百度网盘文件页面";
            public const string NetdiskPathParseError = "无法解析网盘文件路径";
            public const string WebView2InitFailed = "初始化 WebView2 失败: {0}\n\n可能需要安装 WebView2 Runtime。";
            public const string TooManyTabs = "标签页太多了（已超过 {0} 个），请关闭一些标签页后再试。";
        }

        /// <summary>
        /// 配置常量
        /// </summary>
        private static class Config
        {
            public const int MaxTabCount = 8;
            public const int DefaultZoomLevel = 100;
            public const int MinZoomLevel = 50;
            public const int MaxZoomLevel = 300;
            public const int ZoomStep = 10;
            public static string TabStateFilePath => AppPaths.BrowserTabsPath;
        }

        /// <summary>
        /// 标签页状态数据模型
        /// </summary>
        private class TabState
        {
            public List<TabItemState> Tabs { get; set; } = new();
            public int SelectedIndex { get; set; }
        }

        private class TabItemState
        {
            public string Url { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
        }

        /// <summary>
        /// 平台信息映射 (Host -> 显示名称)
        /// </summary>
        private static readonly Dictionary<string, string> ProviderDisplayNames = new()
        {
            ["www.doubao.com"] = "🤖 豆包 (Doubao)",
            ["chat.deepseek.com"] = "🤖 DeepSeek",
            ["chatglm.cn"] = "🤖 智谱AI (Zhipu/GLM)",
            ["tongyi.aliyun.com"] = "🤖 通义千问 (Qwen/DashScope)",
            ["xinghuo.xfyun.cn"] = "🤖 讯飞星火 (Spark)",
            ["yiyan.baidu.com"] = "🤖 文心一言 (ERNIE)"
        };

        /// <summary>
        /// 平台脚本选择器配置 (Host -> CSS选择器)
        /// </summary>
        private static readonly Dictionary<string, string> ProviderScriptSelectors = new()
        {
            ["www.doubao.com"] = "textarea[placeholder*=\"输入\"], textarea",
            ["chat.deepseek.com"] = "textarea, [placeholder*=\"Ask\"]",
            ["chatglm.cn"] = "textarea, [placeholder*=\"问题\"]",
            ["tongyi.aliyun.com"] = "textarea, [placeholder*=\"输入\"]",
            ["xinghuo.xfyun.cn"] = "textarea, [placeholder*=\"输入\"], [placeholder*=\"问题\"]",
            ["yiyan.baidu.com"] = "textarea, [placeholder*=\"输入\"], [placeholder*=\"问题\"]"
        };

        /// <summary>
        /// 初始化时自动填入的提示词
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? InitialPrompt
        {
            get => _initialPrompt;
            set => _initialPrompt = value;
        }

        /// <summary>
        /// 初始化 WebView2 浏览器窗体
        /// </summary>
        /// <param name="cloudStorageService">云存储服务（可选）</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <param name="webBookmarkService">网页书签服务（可选）</param>
        /// <param name="themeService">主题服务（可选）</param>
        public WebView2BrowserForm(ICloudStorageService? cloudStorageService = null,
                                   ILogger? logger = null,
                                   IWebBookmarkService? webBookmarkService = null,
                                   IThemeService? themeService = null,
                                   IPendingContentService? pendingContentService = null)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _webBookmarkService = webBookmarkService;
            _themeService = themeService;
            _pendingContentService = pendingContentService;
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            InitializeProviderButtonMappings();
            InitializeBookmarks();
            Load += WebView2BrowserForm_Load;
            FormClosing += WebView2BrowserForm_FormClosing;

            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.SizeMode = TabSizeMode.Normal;
            tabControl.DrawItem += TabControl_DrawItem;

            _themeService?.RegisterThemeable(this);
        }

        /// <summary>
        /// 初始化平台按钮映射
        /// </summary>
        private void InitializeProviderButtonMappings()
        {
            _providerButtons = new List<ToolStripButton>
            {
                btnProviderDeepseek, btnProviderDoubao, btnProviderZhipu,
                btnProviderQwen, btnProviderSpark, btnProviderWenxin
            };

            _hostToButtonMapping = new Dictionary<string, ToolStripButton>
            {
                ["chat.deepseek.com"] = btnProviderDeepseek,
                ["www.doubao.com"] = btnProviderDoubao,
                ["chatglm.cn"] = btnProviderZhipu,
                ["tongyi.aliyun.com"] = btnProviderQwen,
                ["xinghuo.xfyun.cn"] = btnProviderSpark,
                ["yiyan.baidu.com"] = btnProviderWenxin
            };
        }

        private void InitializeBookmarks()
        {
            if (_webBookmarkService == null || comboBoxBookmarks == null)
                return;

            comboBoxBookmarks.Items.Clear();
            _bookmarkIndexToUrl = new Dictionary<int, string>();

            comboBoxBookmarks.Items.Add("🔖 书签...");

            _cachedBookmarkCategories = _webBookmarkService.GetAllCategories();
            foreach (var category in _cachedBookmarkCategories)
            {
                comboBoxBookmarks.Items.Add($"📁 {category.Name}");
                foreach (var bookmark in category.Bookmarks)
                {
                    int index = comboBoxBookmarks.Items.Add($"  {bookmark.Icon} {bookmark.Title}");
                    _bookmarkIndexToUrl[index] = bookmark.Url;
                }
            }

            comboBoxBookmarks.SelectedIndex = 0;
        }

        private void ComboBoxBookmarks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (comboBoxBookmarks == null || comboBoxBookmarks.SelectedIndex <= 0)
                return;

            int selectedIndex = comboBoxBookmarks.SelectedIndex;
            if (_bookmarkIndexToUrl?.TryGetValue(selectedIndex, out var url) == true)
            {
                NavigateToUrl(url);
            }

            comboBoxBookmarks.SelectedIndex = 0;
        }

        private async void WebView2BrowserForm_Load(object? sender, EventArgs e)
        {
            try
            {
                await EnsureEnvironmentAsync();

                bool restored = await LoadTabStateAsync();
                if (!restored)
                {
                    await CreateNewTabAsync(Urls.BaiduNetdisk, "百度网盘");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "WebView2 初始化过程发生未处理异常");
                MessageBox.Show($"初始化浏览器时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebView2BrowserForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveTabState();
        }

        /// <summary>
        /// 确保 CoreWebView2Environment 已创建
        /// </summary>
        private async Task EnsureEnvironmentAsync()
        {
            if (_webViewEnvironment != null) return;

            var cacheDir = CachePaths.WebView2;
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            _webViewEnvironment = await CoreWebView2Environment.CreateAsync(null, cacheDir);
        }

        /// <summary>
        /// 创建新标签页
        /// </summary>
        /// <param name="url">初始 URL</param>
        /// <param name="title">标签页标题</param>
        /// <returns>创建的 WebView2 控件</returns>
        private async Task<WebView2?> CreateNewTabAsync(string url, string title)
        {
            try
            {
                if (tabControl.TabCount >= Config.MaxTabCount)
                {
                    MessageBox.Show(
                        string.Format(Messages.TooManyTabs, Config.MaxTabCount),
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return null;
                }

                await EnsureEnvironmentAsync();
                if (_webViewEnvironment == null) return null;

                _tabCounter++;
                var tabPage = new TabPage(title)
                {
                    Tag = _tabCounter
                };

                var webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };
                webView.Tag = tabPage;

                tabPage.Controls.Add(webView);
                _webViews[tabPage] = webView;
                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;

                await webView.EnsureCoreWebView2Async(_webViewEnvironment);

                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    webView.CoreWebView2.Settings.IsScriptEnabled = true;
#if DEBUG
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif

                    webView.ZoomFactor = _zoomLevel / 100.0;

                    webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                    webView.NavigationCompleted += WebView_NavigationCompleted;
                    webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                    webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
                    webView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;

                    webView.CoreWebView2.Navigate(url);
                }

                return webView;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建新标签页失败");
                MessageBox.Show($"创建新标签页失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 关闭指定标签页
        /// </summary>
        private async Task CloseTabAsync(TabPage tabPage)
        {
            if (_webViews.TryGetValue(tabPage, out var webView))
            {
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                    webView.NavigationCompleted -= WebView_NavigationCompleted;
                    webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.DocumentTitleChanged -= CoreWebView2_DocumentTitleChanged;
                    webView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                    webView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
                }
                webView.Dispose();
                _webViews.Remove(tabPage);
            }

            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();

            if (tabControl.TabCount == 0)
            {
                try
                {
                    await CreateNewTabAsync(Urls.BaiduNetdisk, "百度网盘");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "关闭标签页后创建新标签页失败");
                }
            }
        }

        private async void CloseTabItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem item && item.Tag is TabPage tabPage)
            {
                try
                {
                    await CloseTabAsync(tabPage);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "关闭标签页失败");
                }
            }
        }

        private async void CloseOtherItem_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem item && item.Tag is TabPage tabPage)
            {
                var tabsToClose = tabControl.TabPages.Cast<TabPage>().Where(t => t != tabPage).ToList();
                foreach (var tab in tabsToClose)
                {
                    try
                    {
                        await CloseTabAsync(tab);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "批量关闭标签页失败");
                    }
                }
            }
        }

        /// <summary>
        /// 新窗口请求处理 - 在新标签页打开
        /// </summary>
        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                e.Handled = true;
                return;
            }

            e.Handled = true;

            var uri = e.Uri;
            BeginInvoke(async () =>
            {
                if (IsDisposed) return;
                try
                {
                    await CreateNewTabAsync(uri, "新标签页");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "处理新窗口请求失败");
                }
            });
        }

        /// <summary>
        /// 文档标题变更处理 - 更新标签页标题
        /// </summary>
        private void CoreWebView2_DocumentTitleChanged(object? sender, object e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            var webView = sender as WebView2;
            if (webView == null) return;

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                UpdateTabPageTitle(webView);
            }));
        }

        private void UpdateTabPageTitle(WebView2 webView)
        {
            var tabPage = webView.Tag as TabPage;
            if (tabPage != null && webView.CoreWebView2 != null)
            {
                var title = webView.CoreWebView2.DocumentTitle;
                if (!string.IsNullOrEmpty(title))
                {
                    if (title.Length > 30)
                        title = title.Substring(0, 30) + "...";
                    tabPage.Text = title;
                }
            }
        }

        /// <summary>
        /// 浏览器进程崩溃处理
        /// </summary>
        private async void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            var webView = sender as WebView2;
            if (webView == null) return;

            _logger?.LogError("WebView2 浏览器进程崩溃，原因：{Reason}", e.ProcessFailedKind);

            BeginInvoke(new Action(async () =>
            {
                if (IsDisposed) return;
                var tabPage = webView.Tag as TabPage;
                if (tabPage != null)
                {
                    try
                    {
                        await CloseTabAsync(tabPage);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "关闭崩溃的标签页失败");
                    }
                }
            }));
        }

        /// <summary>
        /// 导航开始处理 - 显示加载进度
        /// </summary>
        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            var webView = sender as CoreWebView2;
            if (webView == null) return;

            var webViewCtrl = _webViews.FirstOrDefault(kvp => kvp.Value.CoreWebView2 == webView).Value;
            if (webViewCtrl != null && webViewCtrl.Tag is TabPage tabPage && tabPage == tabControl.SelectedTab)
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;
                    ShowLoadingProgress(true);
                }));
            }
        }

        private void ShowLoadingProgress(bool show)
        {
            if (progressBarLoading != null)
            {
                progressBarLoading.Visible = show;
            }
            if (lblLoadingStatus != null)
            {
                lblLoadingStatus.Visible = show;
                lblLoadingStatus.Text = show ? "加载中..." : "";
            }
        }

        /// <summary>
        /// 导航完成处理
        /// </summary>
        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            var webView = sender as WebView2;
            if (webView == null) return;

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                UpdateTabPageTitle(webView);
            }));

            if (webView != CurrentWebView) return;

            BeginInvoke(new Action(async () =>
            {
                if (IsDisposed) return;
                ShowLoadingProgress(false);
                if (CurrentWebView != null)
                {
                    txtUrl.Text = CurrentWebView.Source?.ToString() ?? string.Empty;
                    btnBack.Enabled = CurrentWebView.CanGoBack;
                    btnForward.Enabled = CurrentWebView.CanGoForward;

                    bool isNetdiskPage = CurrentWebView.Source?.ToString()?.StartsWith(Urls.BaiduNetdisk) ?? false;
                    btnOpenNetdisk.Visible = !isNetdiskPage;

                    ResetProviderButtons();

                    string? currentHost = CurrentWebView.Source?.Host;
                    HighlightCurrentProvider(currentHost);

                    if (!string.IsNullOrEmpty(_initialPrompt) && CurrentWebView.CoreWebView2 != null)
                    {
                        await FillPromptAsync(_initialPrompt, currentHost);
                        _initialPrompt = null;
                    }
                }
            }));
        }

        private void CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            var coreWebView2 = sender as CoreWebView2;
            if (coreWebView2 == null) return;

            var menuItems = e.MenuItems;

            var saveToCardItem = coreWebView2.Environment.CreateContextMenuItem(
                "📝 保存为学习卡片",
                null,
                CoreWebView2ContextMenuItemKind.Command);

            var weakWebViewRef = new WeakReference<CoreWebView2>(coreWebView2);

            saveToCardItem.CustomItemSelected += async (s, args) =>
            {
                try
                {
                    if (!weakWebViewRef.TryGetTarget(out var targetWebView))
                    {
                        _logger?.LogWarning("WebView2 已被释放，无法获取选中文本");
                        return;
                    }

                    string? selectionText = await targetWebView.ExecuteScriptAsync("window.getSelection().toString()");
                    if (!string.IsNullOrEmpty(selectionText))
                    {
                        selectionText = System.Text.Json.JsonSerializer.Deserialize<string>(selectionText);
                    }
                    SaveSelectedTextAsCard(selectionText ?? string.Empty, targetWebView.Source);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "获取选中文本失败");
                    MessageBox.Show("获取选中文本失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            int insertIndex = 0;
            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i].Kind == CoreWebView2ContextMenuItemKind.Separator)
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            menuItems.Insert(insertIndex, saveToCardItem);
        }

        private void SaveSelectedTextAsCard(string selectedText, string? sourceUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("请先选择要保存的文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_pendingContentService == null)
                {
                    MessageBox.Show("待添加内容服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string pageTitle = CurrentWebView?.CoreWebView2?.DocumentTitle ?? "网页剪藏";
                string pageUrl = sourceUrl ?? CurrentWebView?.CoreWebView2?.Source ?? string.Empty;

                using var form = new WebClippingSaveForm(selectedText.Trim(), pageTitle, pageUrl);
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    string language = form.SelectedSubject == "英语" ? Constants.Language.English : Constants.Language.Chinese;
                    string category = form.SelectedSubCategory;
                    string content = form.Content.Trim();

                    _pendingContentService.Add(content, language, category);

                    _logger?.LogInformation("已保存划词内容为学习卡片，长度: {Length}, 分类: {Category}", content.Length, category);
                    ShowBalloonTip("已保存", $"已将选中的 {content.Length} 个字符保存到「{category}」");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存划词内容失败");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowBalloonTip(string title, string content)
        {
            MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 标签页切换处理
        /// </summary>
        private async void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (CurrentWebView != null)
            {
                txtUrl.Text = CurrentWebView.Source?.ToString() ?? string.Empty;
                btnBack.Enabled = CurrentWebView.CanGoBack;
                btnForward.Enabled = CurrentWebView.CanGoForward;

                string? currentHost = CurrentWebView.Source?.Host;
                ResetProviderButtons();
                HighlightCurrentProvider(currentHost);

                bool isNetdiskPage = CurrentWebView.Source?.ToString()?.StartsWith(Urls.BaiduNetdisk) ?? false;
                btnOpenNetdisk.Visible = !isNetdiskPage;

                ApplyZoom();
                try
                {
                    await ApplyWebView2ThemeAsync(_currentThemeMode == ThemeMode.Dark);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "切换标签页时应用 WebView2 主题失败");
                }
            }
        }

        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl || e.Index < 0 || e.Index >= tabControl.TabCount)
                return;

            var tabPage = tabControl.TabPages[e.Index];
            var isSelected = tabControl.SelectedIndex == e.Index;
            var tabRect = tabControl.GetTabRect(e.Index);

            var colors = _currentThemeMode == ThemeMode.Dark 
                ? ThemeService.GetColors(ThemeMode.Dark) 
                : ThemeService.GetColors(ThemeMode.Light);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var tabWidth = tabRect.Width;
            var tabHeight = tabRect.Height;
            var cornerRadius = 6;

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddLine(tabRect.Left + cornerRadius, tabRect.Top, tabRect.Right - cornerRadius, tabRect.Top);
            path.AddArc(tabRect.Right - cornerRadius * 2, tabRect.Top, cornerRadius * 2, cornerRadius * 2, -90, 90);
            path.AddLine(tabRect.Right, tabRect.Top + cornerRadius, tabRect.Right, tabRect.Bottom);
            path.AddLine(tabRect.Right, tabRect.Bottom, tabRect.Left, tabRect.Bottom);
            path.AddLine(tabRect.Left, tabRect.Bottom, tabRect.Left, tabRect.Top + cornerRadius);
            path.AddArc(tabRect.Left, tabRect.Top, cornerRadius * 2, cornerRadius * 2, 90, 90);
            path.CloseFigure();

            if (isSelected)
            {
                using var brush = new SolidBrush(colors.Background);
                e.Graphics.FillPath(brush, path);

                using var borderPen = new Pen(colors.Primary, 2);
                e.Graphics.DrawPath(borderPen, path);
            }
            else
            {
                using var brush = new SolidBrush(colors.Surface);
                e.Graphics.FillPath(brush, path);

                using var borderPen = new Pen(colors.Divider);
                e.Graphics.DrawPath(borderPen, path);
            }

            string tabText = tabPage.Text;
            if (tabText.Length > 20)
                tabText = tabText.Substring(0, 20) + "...";

            var textColor = isSelected ? colors.TextPrimary : colors.TextSecondary;
            using var textBrush = new SolidBrush(textColor);
            using var font = new Font("Microsoft YaHei", 9f);

            var textRect = new Rectangle(tabRect.Left + 8, tabRect.Top + 2, tabWidth - 32, tabHeight - 4);
            using var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tabText, font, textBrush, textRect, format);

            var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 4, 14, 14);
            using var closePath = new System.Drawing.Drawing2D.GraphicsPath();
            closePath.AddEllipse(closeRect);

            var closeColor = isSelected ? colors.TextSecondary : colors.TextDisabled;
            using var closeBrush = new SolidBrush(closeColor);
            e.Graphics.FillPath(closeBrush, closePath);

            using var closePen = new Pen(isSelected ? colors.TextPrimary : colors.TextSecondary, 1.5f);
            e.Graphics.DrawLine(closePen, closeRect.Left + 4, closeRect.Top + 4, closeRect.Right - 4, closeRect.Bottom - 4);
            e.Graphics.DrawLine(closePen, closeRect.Right - 4, closeRect.Top + 4, closeRect.Left + 4, closeRect.Bottom - 4);
        }

        /// <summary>
        /// 标签页鼠标点击 - 处理中键关闭和关闭按钮点击
        /// </summary>
        private async void TabControl_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        try
                        {
                            await CloseTabAsync(tabControl.TabPages[i]);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "中键关闭标签页失败");
                        }
                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    var tabRect = tabControl.GetTabRect(i);
                    if (tabRect.Contains(e.Location))
                    {
                        var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 4, 14, 14);
                        if (closeRect.Contains(e.Location))
                        {
                            try
                            {
                                await CloseTabAsync(tabControl.TabPages[i]);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "关闭按钮关闭标签页失败");
                            }
                            break;
                        }
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    if (tabControl.GetTabRect(i).Contains(e.Location))
                    {
                        tabControl.SelectedIndex = i;
                        ShowTabContextMenu(e.Location, tabControl.TabPages[i]);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 显示标签页右键菜单
        /// </summary>
        private void ShowTabContextMenu(Point location, TabPage tabPage)
        {
            using var menu = new ContextMenuStrip();

            var newTabItem = new ToolStripMenuItem("新建标签页");
            newTabItem.Click += async (s, e) => await CreateNewTabAsync(Urls.BaiduNetdisk, "百度网盘");
            menu.Items.Add(newTabItem);

            menu.Items.Add(new ToolStripSeparator());

            var closeTabItem = new ToolStripMenuItem("关闭此标签页");
            closeTabItem.Tag = tabPage;
            closeTabItem.Click += CloseTabItem_Click;
            closeTabItem.Enabled = tabControl.TabCount > 1;
            menu.Items.Add(closeTabItem);

            var closeOtherItem = new ToolStripMenuItem("关闭其他标签页");
            closeOtherItem.Tag = tabPage;
            closeOtherItem.Click += CloseOtherItem_Click;
            closeOtherItem.Enabled = tabControl.TabCount > 1;
            menu.Items.Add(closeOtherItem);

            menu.Show(tabControl, location);
        }

        /// <summary>
        /// 保存标签页状态到本地 JSON 文件
        /// </summary>
        private void SaveTabState()
        {
            try
            {
                var tabState = new TabState
                {
                    SelectedIndex = tabControl.SelectedIndex
                };

                foreach (TabPage tabPage in tabControl.TabPages)
                {
                    if (_webViews.TryGetValue(tabPage, out var webView))
                    {
                        var url = webView.Source?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(url))
                        {
                            tabState.Tabs.Add(new TabItemState
                            {
                                Url = url,
                                Title = tabPage.Text
                            });
                        }
                    }
                }

                if (tabState.Tabs.Count == 0)
                    return;

                var json = JsonConvert.SerializeObject(tabState, Formatting.Indented);
                var directory = Path.GetDirectoryName(Config.TabStateFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(Config.TabStateFilePath, json);
                _logger?.LogInformation("已保存 {Count} 个标签页状态", tabState.Tabs.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存标签页状态失败");
            }
        }

        /// <summary>
        /// 从本地 JSON 文件加载标签页状态并恢复
        /// </summary>
        /// <returns>是否成功恢复了标签页</returns>
        private async Task<bool> LoadTabStateAsync()
        {
            try
            {
                if (!File.Exists(Config.TabStateFilePath))
                    return false;

                var json = File.ReadAllText(Config.TabStateFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var tabState = JsonConvert.DeserializeObject<TabState>(json);
                if (tabState == null || tabState.Tabs.Count == 0)
                    return false;

                _logger?.LogInformation("正在恢复 {Count} 个标签页...", tabState.Tabs.Count);

                for (int i = 0; i < tabState.Tabs.Count; i++)
                {
                    var tab = tabState.Tabs[i];
                    if (string.IsNullOrEmpty(tab.Url))
                        continue;

                    var title = !string.IsNullOrEmpty(tab.Title) ? tab.Title : "新标签页";
                    await CreateNewTabAsync(tab.Url, title);
                }

                if (tabState.SelectedIndex >= 0 && tabState.SelectedIndex < tabControl.TabCount)
                {
                    tabControl.SelectedIndex = tabState.SelectedIndex;
                }

                _logger?.LogInformation("标签页恢复完成，共 {Count} 个", tabControl.TabCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载标签页状态失败");
                return false;
            }
        }

        /// <summary>
        /// 重置所有平台按钮状态
        /// </summary>
        private void ResetProviderButtons()
        {
            if (_providerButtons == null) return;

            foreach (var button in _providerButtons)
            {
                button.BackColor = SystemColors.Control;
                button.Enabled = true;
            }
        }

        /// <summary>
        /// 高亮当前平台按钮
        /// </summary>
        /// <param name="host">当前页面的主机名</param>
        private void HighlightCurrentProvider(string? host)
        {
            ResetProviderButtons();

            if (host != null && ProviderDisplayNames.TryGetValue(host, out var displayName))
            {
                if (_hostToButtonMapping!.TryGetValue(host, out var button))
                {
                    button.BackColor = Color.LightBlue;
                    button.Enabled = false;
                }
                Text = displayName;
            }
            else
            {
                Text = "🌐 WebView2 浏览器";
            }
        }

        /// <summary>
        /// 自动填充提示词到当前平台的输入框
        /// </summary>
        private async Task FillPromptAsync(string prompt, string? host)
        {
            if (CurrentWebView?.CoreWebView2 == null || host == null) return;
            if (!ProviderScriptSelectors.TryGetValue(host, out var selector)) return;

            try
            {
                string escapedPrompt = JsonConvert.SerializeObject(prompt);
                string escapedSelector = JsonConvert.SerializeObject(selector);
                string script = $@"
                    (function() {{
                        var textarea = document.querySelector({escapedSelector});
                        if (textarea) {{
                            textarea.value = {escapedPrompt};
                            textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                            return 'success';
                        }}
                        return 'textarea not found';
                    }})();";

                var result = await CurrentWebView.CoreWebView2.ExecuteScriptAsync(script);
                _logger?.LogDebug("自动填充结果: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "自动填充提示词失败");
            }
        }

        private void ProviderButton_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripButton button && button.Tag is string url)
            {
                NavigateToUrl(url);
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            NavigateToUrl();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            try
            {
                if (CurrentWebView?.CoreWebView2 != null && CurrentWebView.CanGoBack)
                {
                    CurrentWebView.GoBack();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "后退操作失败");
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            try
            {
                if (CurrentWebView?.CoreWebView2 != null && CurrentWebView.CanGoForward)
                {
                    CurrentWebView.GoForward();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "前进操作失败");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                if (CurrentWebView?.CoreWebView2 != null)
                {
                    CurrentWebView.Reload();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新操作失败");
            }
        }

        private async void btnNewTab_Click(object? sender, EventArgs e)
        {
            try
            {
                await CreateNewTabAsync(Urls.BaiduNetdisk, "百度网盘");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "新建标签页失败");
            }
        }

        private void btnZoomIn_Click(object? sender, EventArgs e)
        {
            ZoomIn();
        }

        private void btnZoomOut_Click(object? sender, EventArgs e)
        {
            ZoomOut();
        }

        private void ZoomIn()
        {
            try
            {
                if (_zoomLevel < Config.MaxZoomLevel)
                {
                    _zoomLevel += Config.ZoomStep;
                    ApplyZoom();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "放大操作失败");
            }
        }

        private void ZoomOut()
        {
            try
            {
                if (_zoomLevel > Config.MinZoomLevel)
                {
                    _zoomLevel -= Config.ZoomStep;
                    ApplyZoom();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "缩小操作失败");
            }
        }

        private void ApplyZoom()
        {
            double zoomFactor = _zoomLevel / 100.0;
            foreach (var webView in _webViews.Values)
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    webView.ZoomFactor = zoomFactor;
                }
            }
            if (lblZoom != null)
            {
                lblZoom.Text = $"{_zoomLevel}%";
            }
        }

        private async void btnScreenshot_Click(object? sender, EventArgs e)
        {
            await CaptureRegionScreenshotAsync();
        }

        private async Task<string?> CaptureFullScreenshotBase64Async()
        {
            var currentWebView = CurrentWebView;
            if (currentWebView?.CoreWebView2 == null)
                return null;

            string param = System.Text.Json.JsonSerializer.Serialize(new
            {
                format = "png",
                captureBeyondViewport = true,
                fromSurface = true
            });

            var response = await currentWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", param);
            using var responseDoc = System.Text.Json.JsonDocument.Parse(response);
            return responseDoc.RootElement.GetProperty("data").GetString();
        }

        private async Task<Image?> GetRegionScreenshotAsync()
        {
            var base64 = await CaptureFullScreenshotBase64Async();
            if (string.IsNullOrEmpty(base64))
            {
                MessageBox.Show("截图失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            var bytes = Convert.FromBase64String(base64);
            var fullImage = Image.FromStream(new MemoryStream(bytes));

            try
            {
                using var selectionForm = new RegionSelectionForm(fullImage);
                if (selectionForm.ShowDialog(this) == DialogResult.OK && selectionForm.SelectedRegion.HasValue)
                {
                    var rect = selectionForm.SelectedRegion.Value;
                    if (rect.Width < 5 || rect.Height < 5)
                    {
                        MessageBox.Show("选择的区域太小", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return null;
                    }
                    return CropImage(fullImage, rect);
                }
                return null;
            }
            finally
            {
                fullImage.Dispose();
            }
        }

        private async Task<string?> CaptureRegionScreenshotAsync()
        {
            try
            {
                var currentWebView = CurrentWebView;
                if (currentWebView?.CoreWebView2 == null)
                {
                    MessageBox.Show("当前没有可截图的页面", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }

                using var croppedImage = await GetRegionScreenshotAsync();
                if (croppedImage == null)
                    return null;

                using var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG 图片 (*.png)|*.png",
                    FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    Title = "保存截图"
                };

                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    croppedImage.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    _logger?.LogInformation("区域截图已保存: {FilePath}", saveDialog.FileName);
                    MessageBox.Show($"截图已保存到：\n{saveDialog.FileName}", "截图成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return saveDialog.FileName;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "区域截图失败");
                MessageBox.Show($"截图失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static Image CropImage(Image image, Rectangle rect)
        {
            var cropped = new Bitmap(rect.Width, rect.Height);
            using var g = Graphics.FromImage(cropped);
            g.DrawImage(image, 0, 0, rect, GraphicsUnit.Pixel);
            return cropped;
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateToUrl();
            }
        }

        /// <summary>
        /// 导航到指定URL
        /// </summary>
        /// <param name="url">目标URL，如果为null则使用文本框中的URL</param>
        public void NavigateToUrl(string? url = null)
        {
            try
            {
                url ??= txtUrl.Text;
                var normalizedUrl = NormalizeUrl(url);

                if (string.IsNullOrEmpty(normalizedUrl))
                {
                    MessageBox.Show("请输入有效的网址（URL格式不正确）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var currentWebView = CurrentWebView;
                if (currentWebView == null || currentWebView.CoreWebView2 == null)
                {
                    _logger?.LogWarning("当前 WebView2 不可用，无法导航到：{Url}", normalizedUrl);
                    return;
                }

                currentWebView.CoreWebView2.Navigate(normalizedUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导航失败：{Url}", url);
                MessageBox.Show($"导航失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 标准化URL格式并验证有效性
        /// </summary>
        private static string NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            url = url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return string.Empty;
            }

            return url;
        }

        private void btnOpenNetdisk_Click(object sender, EventArgs e)
        {
            NavigateToUrl(Urls.BaiduNetdisk);
        }

        private void btnOpenInBrowser_Click(object sender, EventArgs e)
        {
            var url = CurrentWebView?.Source?.ToString() ?? txtUrl.Text;
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var psi = new ProcessStartInfo(url)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "在浏览器中打开URL失败: {Url}", url);
                    MessageBox.Show($"无法在浏览器中打开链接: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnAddBookmark_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webBookmarkService == null || CurrentWebView == null)
                {
                    MessageBox.Show("书签服务未配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentUrl = CurrentWebView.Source?.ToString();
                if (string.IsNullOrEmpty(currentUrl))
                {
                    MessageBox.Show("当前页面没有可添加的URL", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var existingBookmark = _webBookmarkService.GetBookmarkByUrl(currentUrl);
                if (existingBookmark != null)
                {
                    MessageBox.Show($"该页面已在书签中：{existingBookmark.Title}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string pageTitle = CurrentWebView.CoreWebView2?.DocumentTitle ?? currentUrl;

                using var bookmarkDialog = new AddBookmarkDialog(
                    pageTitle,
                    currentUrl,
                    _webBookmarkService.GetAllCategories().Select(c => c.Name).ToList());

                if (bookmarkDialog.ShowDialog(this) == DialogResult.OK)
                {
                    var bookmark = new WebBookmarkItem
                    {
                        Title = bookmarkDialog.BookmarkTitle,
                        Url = currentUrl,
                        Icon = "🔗"
                    };

                    _webBookmarkService.AddBookmark(bookmarkDialog.CategoryName, bookmark);
                    InitializeBookmarks();
                    MessageBox.Show($"书签已添加：{bookmark.Title}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "添加书签失败");
                MessageBox.Show($"添加书签失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnManageBookmarks_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webBookmarkService == null)
                {
                    MessageBox.Show("书签服务未配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var managerForm = new BookmarkManagerForm(_webBookmarkService, _logger);
                managerForm.BookmarksChanged += (s, e) =>
                {
                    InitializeBookmarks();
                };
                managerForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开书签管理器失败");
                MessageBox.Show($"打开书签管理器失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService?.UnregisterThemeable(this);
                var webViewList = _webViews.ToList();
                foreach (var kvp in webViewList)
                {
                    var webView = kvp.Value;
                    if (webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                        webView.NavigationCompleted -= WebView_NavigationCompleted;
                        webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                        webView.CoreWebView2.DocumentTitleChanged -= CoreWebView2_DocumentTitleChanged;
                        webView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                        webView.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
                    }
                    webView.Dispose();
                }
                _webViews.Clear();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            _currentThemeMode = colors.ThemeMode;
            BackColor = colors.Background;

            if (toolStrip != null)
            {
                if (colors.ThemeMode == ThemeMode.Dark)
                {
                    toolStrip.BackColor = colors.Surface;
                    toolStrip.ForeColor = colors.TextPrimary;
                    toolStrip.RenderMode = ToolStripRenderMode.Professional;
                    toolStrip.Renderer = new DarkToolStripRenderer(colors);
                }
                else
                {
                    toolStrip.BackColor = SystemColors.Control;
                    toolStrip.ForeColor = SystemColors.ControlText;
                    toolStrip.RenderMode = ToolStripRenderMode.System;
                    toolStrip.Renderer = null;
                }
            }

            if (tabControl != null)
            {
                tabControl.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : SystemColors.Control;
                tabControl.Invalidate();
            }

            if (progressBarLoading != null)
            {
                progressBarLoading.ForeColor = colors.Primary;
                progressBarLoading.BackColor = colors.Background;
            }

            if (lblLoadingStatus != null)
            {
                lblLoadingStatus.ForeColor = colors.TextSecondary;
            }

            async void ApplyThemeAsync()
            {
                try
                {
                    await ApplyWebView2ThemeAsync(colors.ThemeMode == ThemeMode.Dark);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "应用 WebView2 主题失败");
                }
            }
            ApplyThemeAsync();
        }

        private async Task ApplyWebView2ThemeAsync(bool isDark)
        {
            try
            {
                var webViewList = _webViews.ToList();
                foreach (var kvp in webViewList)
                {
                    var webView = kvp.Value;
                    if (webView?.CoreWebView2 == null) continue;

                    string script = isDark
                        ? @"document.documentElement.style.colorScheme = 'dark'; document.documentElement.style.backgroundColor = '#121212';"
                        : @"document.documentElement.style.colorScheme = 'light'; document.documentElement.style.backgroundColor = '';";
                    await webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "应用 WebView2 主题失败");
            }
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private ToolStrip toolStrip;
        private ToolStripButton btnBack;
        private ToolStripButton btnForward;
        private ToolStripButton btnRefresh;
        private ToolStripButton btnNewTab;
        private ToolStripTextBox txtUrl;
        private ToolStripButton btnGo;
        private ToolStripSeparator toolStripSeparator;
        private ToolStripButton btnOpenNetdisk;
        private TabControl tabControl;
        private ToolStripComboBox comboBoxBookmarks;
        private ToolStripSeparator toolStripSeparatorProvider;
        private ToolStripButton btnProviderDeepseek;
        private ToolStripButton btnProviderDoubao;
        private ToolStripButton btnProviderZhipu;
        private ToolStripButton btnProviderQwen;
        private ToolStripButton btnProviderSpark;
        private ToolStripButton btnProviderWenxin;
        private ToolStripButton btnOpenInBrowser;
        private ToolStripButton btnAddBookmark;
        private ToolStripButton btnManageBookmarks;
        private ToolStripSeparator toolStripSeparatorZoom;
        private ToolStripButton btnZoomOut;
        private ToolStripLabel lblZoom;
        private ToolStripButton btnZoomIn;
        private ToolStripSeparator toolStripSeparatorTools;
        private ToolStripButton btnScreenshot;
        private ToolStripProgressBar progressBarLoading;
        private ToolStripLabel lblLoadingStatus;

        private void InitializeComponent()
        {
            toolStrip = new ToolStrip();
            btnBack = new ToolStripButton();
            btnForward = new ToolStripButton();
            btnRefresh = new ToolStripButton();
            btnNewTab = new ToolStripButton();
            comboBoxBookmarks = new ToolStripComboBox();
            txtUrl = new ToolStripTextBox();
            btnGo = new ToolStripButton();
            toolStripSeparatorProvider = new ToolStripSeparator();
            btnProviderDeepseek = new ToolStripButton();
            btnProviderDoubao = new ToolStripButton();
            btnProviderZhipu = new ToolStripButton();
            btnProviderQwen = new ToolStripButton();
            btnProviderSpark = new ToolStripButton();
            btnProviderWenxin = new ToolStripButton();
            toolStripSeparator = new ToolStripSeparator();
            btnOpenNetdisk = new ToolStripButton();
            btnOpenInBrowser = new ToolStripButton();
            btnAddBookmark = new ToolStripButton();
            btnManageBookmarks = new ToolStripButton();
            toolStripSeparatorZoom = new ToolStripSeparator();
            btnZoomOut = new ToolStripButton();
            lblZoom = new ToolStripLabel();
            btnZoomIn = new ToolStripButton();
            toolStripSeparatorTools = new ToolStripSeparator();
            btnScreenshot = new ToolStripButton();
            progressBarLoading = new ToolStripProgressBar();
            lblLoadingStatus = new ToolStripLabel();
            tabControl = new TabControl();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.Items.AddRange(new ToolStripItem[] { btnBack, btnForward, btnRefresh, btnNewTab, comboBoxBookmarks, btnAddBookmark, btnManageBookmarks, toolStripSeparatorZoom, btnZoomOut, lblZoom, btnZoomIn, txtUrl, btnGo, toolStripSeparatorProvider, btnProviderDoubao, btnProviderDeepseek, btnProviderZhipu, btnProviderQwen, btnProviderSpark, btnProviderWenxin, toolStripSeparator, btnOpenNetdisk, toolStripSeparatorTools, btnScreenshot, btnOpenInBrowser, progressBarLoading, lblLoadingStatus });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1160, 25);
            toolStrip.TabIndex = 0;
            toolStrip.Text = "toolStrip1";
            // 
            // btnBack
            // 
            btnBack.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnBack.Enabled = false;
            btnBack.ImageTransparentColor = Color.Magenta;
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(36, 22);
            btnBack.Text = "后退";
            btnBack.Click += btnBack_Click;
            // 
            // btnForward
            // 
            btnForward.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnForward.Enabled = false;
            btnForward.ImageTransparentColor = Color.Magenta;
            btnForward.Name = "btnForward";
            btnForward.Size = new Size(36, 22);
            btnForward.Text = "前进";
            btnForward.Click += btnForward_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRefresh.ImageTransparentColor = Color.Magenta;
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(36, 22);
            btnRefresh.Text = "刷新";
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnNewTab
            // 
            btnNewTab.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNewTab.ImageTransparentColor = Color.Magenta;
            btnNewTab.Name = "btnNewTab";
            btnNewTab.Size = new Size(36, 22);
            btnNewTab.Text = "➕ 新页";
            btnNewTab.ToolTipText = "新建标签页";
            btnNewTab.Click += btnNewTab_Click;
            // 
            // comboBoxBookmarks
            // 
            comboBoxBookmarks.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxBookmarks.Name = "comboBoxBookmarks";
            comboBoxBookmarks.Size = new Size(200, 25);
            comboBoxBookmarks.DropDownWidth = 450;
            comboBoxBookmarks.SelectedIndexChanged += ComboBoxBookmarks_SelectedIndexChanged;
            // 
            // txtUrl
            // 
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(300, 25);
            txtUrl.Text = Urls.BaiduNetdisk;
            txtUrl.KeyDown += txtUrl_KeyDown;
            // 
            // btnGo
            // 
            btnGo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnGo.ImageTransparentColor = Color.Magenta;
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(36, 22);
            btnGo.Text = "跳转";
            btnGo.Click += btnGo_Click;
            // 
            // toolStripSeparatorProvider
            // 
            toolStripSeparatorProvider.Name = "toolStripSeparatorProvider";
            toolStripSeparatorProvider.Size = new Size(6, 25);
            // 
            // btnProviderDeepseek
            // 
            btnProviderDeepseek.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderDeepseek.ImageTransparentColor = Color.Magenta;
            btnProviderDeepseek.Name = "btnProviderDeepseek";
            btnProviderDeepseek.Size = new Size(71, 22);
            btnProviderDeepseek.Text = "DeepSeek";
            btnProviderDeepseek.Tag = Urls.DeepSeek;
            btnProviderDeepseek.Click += ProviderButton_Click;
            // 
            // btnProviderDoubao
            // 
            btnProviderDoubao.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderDoubao.ImageTransparentColor = Color.Magenta;
            btnProviderDoubao.Name = "btnProviderDoubao";
            btnProviderDoubao.Size = new Size(36, 22);
            btnProviderDoubao.Text = "豆包";
            btnProviderDoubao.Tag = Urls.Doubao;
            btnProviderDoubao.Click += ProviderButton_Click;
            // 
            // btnProviderZhipu
            // 
            btnProviderZhipu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderZhipu.ImageTransparentColor = Color.Magenta;
            btnProviderZhipu.Name = "btnProviderZhipu";
            btnProviderZhipu.Size = new Size(36, 22);
            btnProviderZhipu.Text = "智谱";
            btnProviderZhipu.Tag = Urls.Zhipu;
            btnProviderZhipu.Click += ProviderButton_Click;
            // 
            // btnProviderQwen
            // 
            btnProviderQwen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderQwen.ImageTransparentColor = Color.Magenta;
            btnProviderQwen.Name = "btnProviderQwen";
            btnProviderQwen.Size = new Size(36, 22);
            btnProviderQwen.Text = "千问";
            btnProviderQwen.Tag = Urls.Qwen;
            btnProviderQwen.Click += ProviderButton_Click;
            // 
            // btnProviderSpark
            // 
            btnProviderSpark.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderSpark.ImageTransparentColor = Color.Magenta;
            btnProviderSpark.Name = "btnProviderSpark";
            btnProviderSpark.Size = new Size(36, 22);
            btnProviderSpark.Text = "讯飞";
            btnProviderSpark.Tag = Urls.Spark;
            btnProviderSpark.Click += ProviderButton_Click;
            // 
            // btnProviderWenxin
            // 
            btnProviderWenxin.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderWenxin.ImageTransparentColor = Color.Magenta;
            btnProviderWenxin.Name = "btnProviderWenxin";
            btnProviderWenxin.Size = new Size(36, 22);
            btnProviderWenxin.Text = "文心";
            btnProviderWenxin.Tag = Urls.Wenxin;
            btnProviderWenxin.Click += ProviderButton_Click;
            // 
            // toolStripSeparator
            // 
            toolStripSeparator.Name = "toolStripSeparator";
            toolStripSeparator.Size = new Size(6, 25);
            // 
            // btnOpenNetdisk
            // 
            btnOpenNetdisk.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnOpenNetdisk.ImageTransparentColor = Color.Magenta;
            btnOpenNetdisk.Name = "btnOpenNetdisk";
            btnOpenNetdisk.Size = new Size(60, 22);
            btnOpenNetdisk.Text = "百度网盘";
            btnOpenNetdisk.Click += btnOpenNetdisk_Click;
            // 
            // btnOpenInBrowser
            // 
            btnOpenInBrowser.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnOpenInBrowser.ImageTransparentColor = Color.Magenta;
            btnOpenInBrowser.Name = "btnOpenInBrowser";
            btnOpenInBrowser.Size = new Size(84, 22);
            btnOpenInBrowser.Text = "🌐 在浏览器中打开";
            btnOpenInBrowser.Click += btnOpenInBrowser_Click;
            // 
            // btnAddBookmark
            // 
            btnAddBookmark.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAddBookmark.ImageTransparentColor = Color.Magenta;
            btnAddBookmark.Name = "btnAddBookmark";
            btnAddBookmark.Size = new Size(36, 22);
            btnAddBookmark.Text = "⭐ 添加";
            btnAddBookmark.ToolTipText = "添加当前页面到书签";
            btnAddBookmark.Click += btnAddBookmark_Click;
            // 
            // btnManageBookmarks
            // 
            btnManageBookmarks.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnManageBookmarks.ImageTransparentColor = Color.Magenta;
            btnManageBookmarks.Name = "btnManageBookmarks";
            btnManageBookmarks.Size = new Size(36, 22);
            btnManageBookmarks.Text = "📚 管理";
            btnManageBookmarks.ToolTipText = "书签管理器";
            btnManageBookmarks.Click += btnManageBookmarks_Click;
            // 
            // toolStripSeparatorZoom
            // 
            toolStripSeparatorZoom.Name = "toolStripSeparatorZoom";
            toolStripSeparatorZoom.Size = new Size(6, 25);
            // 
            // btnZoomOut
            // 
            btnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomOut.ImageTransparentColor = Color.Magenta;
            btnZoomOut.Name = "btnZoomOut";
            btnZoomOut.Size = new Size(23, 22);
            btnZoomOut.Text = "−";
            btnZoomOut.ToolTipText = "缩小";
            btnZoomOut.Click += btnZoomOut_Click;
            // 
            // lblZoom
            // 
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(48, 22);
            lblZoom.Text = Config.DefaultZoomLevel + "%";
            lblZoom.ToolTipText = "缩放比例";
            // 
            // btnZoomIn
            // 
            btnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomIn.ImageTransparentColor = Color.Magenta;
            btnZoomIn.Name = "btnZoomIn";
            btnZoomIn.Size = new Size(23, 22);
            btnZoomIn.Text = "+";
            btnZoomIn.ToolTipText = "放大";
            btnZoomIn.Click += btnZoomIn_Click;
            // 
            // toolStripSeparatorTools
            // 
            toolStripSeparatorTools.Name = "toolStripSeparatorTools";
            toolStripSeparatorTools.Size = new Size(6, 25);
            // 
            // btnScreenshot
            // 
            btnScreenshot.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnScreenshot.ImageTransparentColor = Color.Magenta;
            btnScreenshot.Name = "btnScreenshot";
            btnScreenshot.Size = new Size(60, 22);
            btnScreenshot.Text = "📷 截图";
            btnScreenshot.ToolTipText = "截取当前页面";
            btnScreenshot.Click += btnScreenshot_Click;
            // 
            // progressBarLoading
            // 
            progressBarLoading.Name = "progressBarLoading";
            progressBarLoading.Size = new Size(120, 18);
            progressBarLoading.Visible = false;
            progressBarLoading.Style = ProgressBarStyle.Marquee;
            progressBarLoading.MarqueeAnimationSpeed = 30;
            // 
            // lblLoadingStatus
            // 
            lblLoadingStatus.Name = "lblLoadingStatus";
            lblLoadingStatus.Size = new Size(80, 22);
            lblLoadingStatus.Text = "";
            lblLoadingStatus.Visible = false;
            // 
            // tabControl
            // 
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 25);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1160, 577);
            tabControl.TabIndex = 1;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.MouseDown += TabControl_MouseDown;
            // 
            // WebView2BrowserForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1160, 602);
            Controls.Add(tabControl);
            Controls.Add(toolStrip);
            Name = "WebView2BrowserForm";
            Text = "WebView2 浏览器";
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }

    internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemeColors _colors;

        public DarkToolStripRenderer(ThemeColors colors)
        {
            _colors = colors;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(_colors.Surface);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
            
            using var borderPen = new Pen(_colors.Divider);
            e.Graphics.DrawLine(borderPen, e.AffectedBounds.Left, e.AffectedBounds.Bottom - 1, 
                e.AffectedBounds.Right, e.AffectedBounds.Bottom - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using var brush = new SolidBrush(_colors.Primary);
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = e.Item.ContentRectangle;
            rect.Inflate(-1, -1);

            if (e.Item.Selected || e.Item.Pressed)
            {
                using var brush = new SolidBrush(_colors.PrimaryLight);
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var rect = e.Item.ContentRectangle;
            var centerY = rect.Y + rect.Height / 2;
            
            using var pen = new Pen(_colors.Divider);
            e.Graphics.DrawLine(pen, rect.X + 4, centerY, rect.X + rect.Width - 4, centerY);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }

        protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e)
        {
        }
    }

    /// <summary>
    /// 区域选择窗体 - 用于截图时框选区域
    /// </summary>
    internal class RegionSelectionForm : Form
    {
        private readonly Image _image;
        private Point _startPoint;
        private Point _endPoint;
        private bool _isSelecting;
        private Rectangle _imageRect;
        public Rectangle? SelectedRegion { get; private set; }

        public RegionSelectionForm(Image image)
        {
            _image = image;
            _imageRect = Rectangle.Empty;
            InitializeComponent();
            Text = "拖拽选择截图区域（按 Enter 确认，Esc 取消）";
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Cross;
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 17F);
            ClientSize = new Size(800, 600);
            Name = "RegionSelectionForm";
            KeyDown += RegionSelectionForm_KeyDown;
            MouseDown += RegionSelectionForm_MouseDown;
            MouseMove += RegionSelectionForm_MouseMove;
            MouseUp += RegionSelectionForm_MouseUp;
            Paint += RegionSelectionForm_Paint;
            Resize += RegionSelectionForm_Resize;
            Shown += RegionSelectionForm_Shown;
            ResumeLayout(false);
        }

        private void RegionSelectionForm_Shown(object? sender, EventArgs e)
        {
            CalculateImageRect();
            Invalidate();
        }

        private void RegionSelectionForm_Resize(object? sender, EventArgs e)
        {
            CalculateImageRect();
            Invalidate();
        }

        private void CalculateImageRect()
        {
            if (_image == null || ClientSize.Width == 0 || ClientSize.Height == 0)
                return;

            float imgRatio = (float)_image.Width / _image.Height;
            float screenRatio = (float)ClientSize.Width / ClientSize.Height;

            int width, height;
            if (imgRatio > screenRatio)
            {
                width = ClientSize.Width;
                height = (int)(width / imgRatio);
            }
            else
            {
                height = ClientSize.Height;
                width = (int)(height * imgRatio);
            }

            int x = (ClientSize.Width - width) / 2;
            int y = (ClientSize.Height - height) / 2;
            _imageRect = new Rectangle(x, y, width, height);
        }

        private void RegionSelectionForm_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            if (_image != null && _imageRect.Width > 0)
            {
                e.Graphics.DrawImage(_image, _imageRect);
            }

            using var brush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            var rect = GetSelectionRect();

            if (rect.Width > 0 && rect.Height > 0)
            {
                var regions = new[]
                {
                    new Rectangle(0, 0, ClientSize.Width, rect.Top),
                    new Rectangle(0, rect.Bottom, ClientSize.Width, ClientSize.Height - rect.Bottom),
                    new Rectangle(0, rect.Top, rect.Left, rect.Height),
                    new Rectangle(rect.Right, rect.Top, ClientSize.Width - rect.Right, rect.Height)
                };
                foreach (var r in regions)
                {
                    e.Graphics.FillRectangle(brush, r);
                }

                using var pen = new Pen(Color.Red, 2);
                e.Graphics.DrawRectangle(pen, rect);

                var originalRect = ScreenToImage(rect);
                var text = $"{originalRect.Width} x {originalRect.Height}";
                var textSize = e.Graphics.MeasureString(text, Font);
                var textRect = new RectangleF(rect.X, rect.Y - (int)textSize.Height - 5, (int)textSize.Width + 10, (int)textSize.Height + 4);
                if (textRect.Y < 0)
                    textRect.Y = rect.Y + 5;
                if (textRect.Right > ClientSize.Width)
                    textRect.X = ClientSize.Width - textRect.Width;

                e.Graphics.FillRectangle(Brushes.Black, textRect);
                e.Graphics.DrawString(text, Font, Brushes.White, textRect.X + 5, textRect.Y + 2);
            }
            else
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);

                var tipText = "拖拽鼠标选择截图区域（Enter 确认，Esc 取消）";
                using var tipFont = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
                var tipSize = e.Graphics.MeasureString(tipText, tipFont);
                var tipX = (ClientSize.Width - tipSize.Width) / 2;
                var tipY = (ClientSize.Height - tipSize.Height) / 2;
                e.Graphics.DrawString(tipText, tipFont, Brushes.White, tipX, tipY);
            }
        }

        private Rectangle GetSelectionRect()
        {
            int x = Math.Min(_startPoint.X, _endPoint.X);
            int y = Math.Min(_startPoint.Y, _endPoint.Y);
            int width = Math.Abs(_endPoint.X - _startPoint.X);
            int height = Math.Abs(_endPoint.Y - _startPoint.Y);
            return new Rectangle(x, y, width, height);
        }

        private Rectangle ScreenToImage(Rectangle screenRect)
        {
            if (_imageRect.Width == 0 || _imageRect.Height == 0 || _image == null)
                return screenRect;

            float scaleX = (float)_image.Width / _imageRect.Width;
            float scaleY = (float)_image.Height / _imageRect.Height;

            int x = (int)((screenRect.X - _imageRect.X) * scaleX);
            int y = (int)((screenRect.Y - _imageRect.Y) * scaleY);
            int width = (int)(screenRect.Width * scaleX);
            int height = (int)(screenRect.Height * scaleY);

            x = Math.Clamp(x, 0, _image.Width);
            y = Math.Clamp(y, 0, _image.Height);
            width = Math.Clamp(width, 0, _image.Width - x);
            height = Math.Clamp(height, 0, _image.Height - y);

            return new Rectangle(x, y, width, height);
        }

        private void RegionSelectionForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _startPoint = e.Location;
                _endPoint = e.Location;
                _isSelecting = true;
                Invalidate();
            }
        }

        private void RegionSelectionForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                _endPoint = e.Location;
                Invalidate();
            }
        }

        private void RegionSelectionForm_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_isSelecting && e.Button == MouseButtons.Left)
            {
                _endPoint = e.Location;
                _isSelecting = false;
                var screenRect = GetSelectionRect();
                if (screenRect.Width > 10 && screenRect.Height > 10)
                {
                    SelectedRegion = ScreenToImage(screenRect);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                Invalidate();
            }
        }

        private void RegionSelectionForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                var screenRect = GetSelectionRect();
                if (screenRect.Width > 10 && screenRect.Height > 10)
                {
                    SelectedRegion = ScreenToImage(screenRect);
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
