using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// WebView2 浏览器窗体，支持多个 AI 平台快捷导航和百度网盘集成
    /// </summary>
    public partial class WebView2BrowserForm : Form, IThemeable
    {
        private readonly ICloudStorageService? _cloudStorageService;
        private readonly ILogger? _logger;
        private readonly IWebBookmarkService? _webBookmarkService;
        private readonly IThemeService? _themeService;
        private WebView2? _webView;
        private bool _isWebViewReady = false;
        private string? _initialPrompt;
        private IReadOnlyList<WebBookmarkCategory>? _cachedBookmarkCategories;
        private List<ToolStripButton>? _providerButtons;
        private Dictionary<string, ToolStripButton>? _hostToButtonMapping;
        private Dictionary<int, string>? _bookmarkIndexToUrl;
        private ThemeMode _currentThemeMode = ThemeMode.Light;

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
                                   IThemeService? themeService = null)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _webBookmarkService = webBookmarkService;
            _themeService = themeService;
            InitializeComponent();
            InitializeProviderButtonMappings();
            InitializeBookmarks();
            Load += WebView2BrowserForm_Load;

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

            // 添加占位符项
            comboBoxBookmarks.Items.Add("🔖 书签...");

            _cachedBookmarkCategories = _webBookmarkService.GetAllCategories();
            foreach (var category in _cachedBookmarkCategories)
            {
                // 添加分类标题（不可点击）
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
                await InitializeWebViewAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "WebView2 初始化过程发生未处理异常");
                MessageBox.Show($"初始化浏览器时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task InitializeWebViewAsync()
        {
            const int maxRetryCount = 3;
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < maxRetryCount)
            {
                try
                {
                    var cacheDir = CachePaths.WebView2;
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }

                    var environment = await CoreWebView2Environment.CreateAsync(null, cacheDir);

                    _webView = new WebView2
                    {
                        Dock = DockStyle.Fill
                    };

                    await _webView.EnsureCoreWebView2Async(environment);

                    if (_webView.CoreWebView2 != null)
                    {
                        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                        _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                        _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                        _webView.Source = new Uri(Urls.Baidu);

                        _webView.NavigationCompleted += OnNavigationCompleted;

                        panelBrowser.Controls.Add(_webView);
                        _isWebViewReady = true;
                        success = true;
                    }
                    else
                    {
                        throw new InvalidOperationException("CoreWebView2 初始化失败");
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger?.LogError(ex, $"WebView2 初始化失败 (尝试 {retryCount}/{maxRetryCount})");

                    if (retryCount >= maxRetryCount)
                    {
                        MessageBox.Show(string.Format(Messages.WebView2InitFailed, ex.Message), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _logger?.LogError(ex, "WebView2 初始化最终失败");
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
            }
        }

        private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke(new Action(async () =>
            {
                if (_webView != null)
                {
                    txtUrl.Text = _webView.Source?.ToString() ?? string.Empty;
                    btnBack.Enabled = _webView.CanGoBack;
                    btnForward.Enabled = _webView.CanGoForward;

                    bool isNetdiskPage = _webView.Source?.ToString()?.StartsWith(Urls.BaiduNetdisk) ?? false;
                    btnOpenNetdisk.Visible = !isNetdiskPage;
                    btnDownloadNetdisk.Visible = isNetdiskPage && _cloudStorageService != null && _cloudStorageService.IsAuthenticated;

                    // 重置所有平台按钮
                    ResetProviderButtons();

                    // 高亮当前平台按钮
                    string? currentHost = _webView.Source?.Host;
                    HighlightCurrentProvider(currentHost);

                    // 自动填充提示词
                    if (!string.IsNullOrEmpty(_initialPrompt) && _webView.CoreWebView2 != null)
                    {
                        await FillPromptAsync(_initialPrompt, currentHost);
                        _initialPrompt = null; // 只填充一次
                    }
                }
            }));
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
        /// <param name="prompt">要填充的提示词</param>
        /// <param name="host">当前页面的主机名</param>
        private async Task FillPromptAsync(string prompt, string? host)
        {
            if (_webView?.CoreWebView2 == null || host == null) return;
            if (!ProviderScriptSelectors.TryGetValue(host, out var selector)) return;

            try
            {
                // 使用 JSON 序列化安全传递 prompt，避免注入风险
                string escapedPrompt = JsonSerializer.Serialize(prompt);
                string script = $@"
                    (function() {{
                        var textarea = document.querySelector('{selector}');
                        if (textarea) {{
                            textarea.value = {escapedPrompt};
                            textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                            return 'success';
                        }}
                        return 'textarea not found';
                    }})();";

                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
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
                _webView.Source = new Uri(url);
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            NavigateToUrl();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (_webView?.CanGoBack == true)
            {
                _webView.GoBack();
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (_webView?.CanGoForward == true)
            {
                _webView.GoForward();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _webView?.Reload();
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
            url ??= txtUrl.Text;
            var normalizedUrl = NormalizeUrl(url);

            if (!string.IsNullOrEmpty(normalizedUrl) && _webView != null)
            {
                _webView.Source = new Uri(normalizedUrl);
            }
        }

        /// <summary>
        /// 标准化URL格式
        /// </summary>
        /// <param name="url">原始URL</param>
        /// <returns>标准化后的URL</returns>
        private static string NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            url = url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            return url;
        }

        private void btnOpenNetdisk_Click(object sender, EventArgs e)
        {
            if (_webView != null)
            {
                _webView.Source = new Uri(Urls.BaiduNetdisk);
            }
        }

        private async void btnDownloadNetdisk_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cloudStorageService == null)
                {
                    MessageBox.Show(Messages.NetdiskNotConfigured, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_cloudStorageService.IsAuthenticated)
                {
                    MessageBox.Show(Messages.NetdiskNotAuthorized, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentUrl = _webView?.Source?.ToString();
                if (string.IsNullOrEmpty(currentUrl) || !currentUrl.StartsWith(Urls.BaiduNetdisk))
                {
                    MessageBox.Show(Messages.NetdiskNavigatePrompt, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var fileInfo = ParseNetdiskUrl(currentUrl);
                if (!fileInfo.HasValue)
                {
                    MessageBox.Show(Messages.NetdiskPathParseError, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var (cloudPath, fileName) = fileInfo.Value;

                using var saveDialog = new SaveFileDialog
                {
                    Filter = "所有文件 (*.*)|*.*",
                    FileName = fileName,
                    Title = "保存网盘文件"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using var progressForm = new ProgressForm();
                    progressForm.ShowDialog(this);

                    bool success = await _cloudStorageService.DownloadFileAsync(
                        cloudPath,
                        saveDialog.FileName,
                        (progress) =>
                        {
                            progressForm.UpdateProgress(progress);
                        });

                    if (success)
                    {
                        MessageBox.Show($"文件下载成功！\n\n保存位置: {saveDialog.FileName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("文件下载失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "下载网盘文件失败");
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private (string CloudPath, string FileName)? ParseNetdiskUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                var path = query["path"];
                if (!string.IsNullOrEmpty(path))
                {
                    return (path, Path.GetFileName(path));
                }

                var segments = uri.Segments;
                if (segments.Length >= 3)
                {
                    if (segments[1].Equals("s/", StringComparison.OrdinalIgnoreCase))
                    {
                        var shareId = segments[2].TrimEnd('/');
                        return ($"/share/{shareId}", shareId);
                    }
                }

                return ("/", "root");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "解析网盘URL失败: {Url}", url);
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService?.UnregisterThemeable(this);
                if (_webView != null)
                {
                    _webView.NavigationCompleted -= OnNavigationCompleted;
                    _webView.Dispose();
                    _webView = null;
                }
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

            if (panelBrowser != null)
            {
                panelBrowser.BackColor = colors.Surface;
            }

            // 通知 WebView2 页面主题变更
            ApplyWebView2ThemeAsync(colors.ThemeMode == ThemeMode.Dark);
        }

        private async void ApplyWebView2ThemeAsync(bool isDark)
        {
            if (_webView?.CoreWebView2 == null) return;

            try
            {
                string script = isDark
                    ? @"document.documentElement.style.colorScheme = 'dark'; document.documentElement.style.backgroundColor = '#121212';"
                    : @"document.documentElement.style.colorScheme = 'light'; document.documentElement.style.backgroundColor = '';";
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
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
        private ToolStripTextBox txtUrl;
        private ToolStripButton btnGo;
        private ToolStripSeparator toolStripSeparator;
        private ToolStripButton btnOpenNetdisk;
        private ToolStripButton btnDownloadNetdisk;
        private Panel panelBrowser;
        private ToolStripComboBox comboBoxBookmarks;
        private ToolStripSeparator toolStripSeparatorProvider;
        private ToolStripButton btnProviderDeepseek;
        private ToolStripButton btnProviderDoubao;
        private ToolStripButton btnProviderZhipu;
        private ToolStripButton btnProviderQwen;
        private ToolStripButton btnProviderSpark;
        private ToolStripButton btnProviderWenxin;

        private void InitializeComponent()
        {
            toolStrip = new ToolStrip();
            btnBack = new ToolStripButton();
            btnForward = new ToolStripButton();
            btnRefresh = new ToolStripButton();
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
            btnDownloadNetdisk = new ToolStripButton();
            panelBrowser = new Panel();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.Items.AddRange(new ToolStripItem[] { btnBack, btnForward, btnRefresh, comboBoxBookmarks, txtUrl, btnGo, toolStripSeparatorProvider, btnProviderDoubao, btnProviderDeepseek, btnProviderZhipu, btnProviderQwen, btnProviderSpark, btnProviderWenxin, toolStripSeparator, btnOpenNetdisk, btnDownloadNetdisk });
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
            // comboBoxBookmarks
            // 
            comboBoxBookmarks.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxBookmarks.Name = "comboBoxBookmarks";
            comboBoxBookmarks.Size = new Size(280, 25);
            comboBoxBookmarks.DropDownWidth = 450;
            comboBoxBookmarks.SelectedIndexChanged += ComboBoxBookmarks_SelectedIndexChanged;
            // 
            // txtUrl
            // 
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(400, 25);
            txtUrl.Text = Urls.Baidu;
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
            // btnDownloadNetdisk
            // 
            btnDownloadNetdisk.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDownloadNetdisk.ImageTransparentColor = Color.Magenta;
            btnDownloadNetdisk.Name = "btnDownloadNetdisk";
            btnDownloadNetdisk.Size = new Size(60, 22);
            btnDownloadNetdisk.Text = "下载文件";
            btnDownloadNetdisk.Visible = false;
            btnDownloadNetdisk.Click += btnDownloadNetdisk_Click;
            // 
            // panelBrowser
            // 
            panelBrowser.Dock = DockStyle.Fill;
            panelBrowser.Location = new Point(0, 25);
            panelBrowser.Name = "panelBrowser";
            panelBrowser.Size = new Size(1160, 577);
            panelBrowser.TabIndex = 1;
            // 
            // WebView2BrowserForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1160, 602);
            Controls.Add(panelBrowser);
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

    /// <summary>
    /// 深色主题的 ToolStrip 渲染器
    /// </summary>
    internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemeColors _colors;

        public DarkToolStripRenderer(ThemeColors colors) : base(new DarkColorTable(colors))
        {
            _colors = colors;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is ToolStripButton btn && btn.Enabled)
            {
                e.TextColor = _colors.TextPrimary;
            }
            else
            {
                e.TextColor = _colors.TextSecondary;
            }
            base.OnRenderItemText(e);
        }
    }

    /// <summary>
    /// 深色主题 ToolStrip 颜色表
    /// </summary>
    internal class DarkColorTable : ProfessionalColorTable
    {
        private readonly ThemeColors _colors;

        public DarkColorTable(ThemeColors colors)
        {
            _colors = colors;
        }

        public override Color ToolStripBorder => _colors.Divider;
        public override Color ToolStripContentPanelGradientBegin => _colors.Surface;
        public override Color ToolStripContentPanelGradientEnd => _colors.Surface;
        public override Color ToolStripGradientBegin => _colors.Surface;
        public override Color ToolStripGradientEnd => _colors.Surface;
        public override Color ToolStripGradientMiddle => _colors.Surface;
        public override Color ButtonSelectedGradientBegin => _colors.Primary;
        public override Color ButtonSelectedGradientEnd => _colors.Primary;
        public override Color ButtonCheckedGradientBegin => _colors.Primary;
        public override Color ButtonCheckedGradientEnd => _colors.Primary;
        public override Color ButtonPressedGradientBegin => _colors.PrimaryDark;
        public override Color ButtonPressedGradientEnd => _colors.PrimaryDark;
        public override Color MenuItemBorder => _colors.Divider;
        public override Color MenuItemSelected => _colors.Primary;
    }
}