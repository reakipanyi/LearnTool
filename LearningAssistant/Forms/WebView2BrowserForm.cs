using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;

namespace LearningAssistant.Forms
{
    public partial class WebView2BrowserForm : Form
    {
        private readonly ICloudStorageService? _cloudStorageService;
        private readonly ILogger<WebView2BrowserForm>? _logger;
        private readonly IWebBookmarkService? _webBookmarkService;
        private WebView2? _webView;
        private const string BaiduNetdiskUrl = "https://pan.baidu.com";
        private bool _isWebViewReady = false;
        private string? _initialPrompt;

        /// <summary>
        /// 初始化时自动填入的提示词
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? InitialPrompt
        {
            get => _initialPrompt;
            set => _initialPrompt = value;
        }

        public WebView2BrowserForm(ICloudStorageService? cloudStorageService = null,
                                   ILogger<WebView2BrowserForm>? logger = null,
                                   IWebBookmarkService? webBookmarkService = null)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _webBookmarkService = webBookmarkService;
            InitializeComponent();
            InitializeBookmarks();
            Load += WebView2BrowserForm_Load;
        }

        private void InitializeBookmarks()
        {
            if (_webBookmarkService == null || comboBoxBookmarks == null)
                return;

            comboBoxBookmarks.Items.Clear();
            comboBoxBookmarks.Items.Add("🔖 书签...");

            var categories = _webBookmarkService.GetAllCategories();
            foreach (var category in categories)
            {
                comboBoxBookmarks.Items.Add($"📁 {category.Name}");
                foreach (var bookmark in category.Bookmarks)
                {
                    comboBoxBookmarks.Items.Add($"  {bookmark.Icon} {bookmark.Title}");
                }
            }

            comboBoxBookmarks.SelectedIndex = 0;
        }

        private void ComboBoxBookmarks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_webBookmarkService == null || comboBoxBookmarks == null || comboBoxBookmarks.SelectedIndex <= 0)
                return;

            var categories = _webBookmarkService.GetAllCategories();
            int currentIndex = comboBoxBookmarks.SelectedIndex - 1;

            foreach (var category in categories)
            {
                if (currentIndex < category.Bookmarks.Count + 1)
                {
                    if (currentIndex > 0)
                    {
                        var bookmark = category.Bookmarks[currentIndex - 1];
                        NavigateToUrl(bookmark.Url);
                    }
                    break;
                }
                currentIndex -= category.Bookmarks.Count + 1;
            }

            comboBoxBookmarks.SelectedIndex = 0;
        }

        private async void WebView2BrowserForm_Load(object? sender, EventArgs e)
        {
            await InitializeWebViewAsync();
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
                    var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var cacheDir = Path.Combine(appDataDir, "LearningAssistant", "webview2_cache");
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

                        _webView.Source = new Uri("https://www.baidu.com");

                        _webView.NavigationCompleted += OnNavigationCompleted;
                        // 使用 NavigationCompleted 事件替代 TitleChanged（兼容旧版本 WebView2）
                        // _webView.CoreWebView2.TitleChanged += OnTitleChanged;

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
                        MessageBox.Show($"初始化 WebView2 失败: {ex.Message}\n\n可能需要安装 WebView2 Runtime。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    bool isNetdiskPage = _webView.Source?.ToString()?.StartsWith("https://pan.baidu.com") ?? false;
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

        private void ResetProviderButtons()
        {
            btnProviderDeepseek.BackColor = SystemColors.Control;
            btnProviderDeepseek.Enabled = true;
            btnProviderDoubao.BackColor = SystemColors.Control;
            btnProviderDoubao.Enabled = true;
            btnProviderZhipu.BackColor = SystemColors.Control;
            btnProviderZhipu.Enabled = true;
            btnProviderQwen.BackColor = SystemColors.Control;
            btnProviderQwen.Enabled = true;
            btnProviderSpark.BackColor = SystemColors.Control;
            btnProviderSpark.Enabled = true;
            btnProviderWenxin.BackColor = SystemColors.Control;
            btnProviderWenxin.Enabled = true;
        }

        private void HighlightCurrentProvider(string? host)
        {
            if (host == "chat.deepseek.com")
            {
                btnProviderDeepseek.BackColor = Color.LightBlue;
                btnProviderDeepseek.Enabled = false;
                Text = "🤖 DeepSeek";
            }
            else if (host == "www.doubao.com")
            {
                btnProviderDoubao.BackColor = Color.LightBlue;
                btnProviderDoubao.Enabled = false;
                Text = "🤖 豆包 (Doubao)";
            }
            else if (host == "chatglm.cn")
            {
                btnProviderZhipu.BackColor = Color.LightBlue;
                btnProviderZhipu.Enabled = false;
                Text = "🤖 智谱AI (Zhipu/GLM)";
            }
            else if (host == "tongyi.aliyun.com")
            {
                btnProviderQwen.BackColor = Color.LightBlue;
                btnProviderQwen.Enabled = false;
                Text = "🤖 通义千问 (Qwen/DashScope)";
            }
            else if (host == "xinghuo.xfyun.cn")
            {
                btnProviderSpark.BackColor = Color.LightBlue;
                btnProviderSpark.Enabled = false;
                Text = "🤖 讯飞星火 (Spark)";
            }
            else if (host == "yiyan.baidu.com")
            {
                btnProviderWenxin.BackColor = Color.LightBlue;
                btnProviderWenxin.Enabled = false;
                Text = "🤖 文心一言 (ERNIE)";
            }
            else
            {
                Text = "🌐 WebView2 浏览器";
            }
        }

        private async Task FillPromptAsync(string prompt, string? host)
        {
            if (_webView?.CoreWebView2 == null) return;

            try
            {
                string script = host switch
                {
                    // 豆包
                    "www.doubao.com" => $@"
                        (function() {{
                            var textarea = document.querySelector('textarea[placeholder*='输入']') || document.querySelector('textarea');
                            if (textarea) {{
                                textarea.value = '{EscapeForJavaScript(prompt)}';
                                textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                return 'success';
                            }}
                            return 'textarea not found';
                        }})();
                    ",
                    // DeepSeek
                    "chat.deepseek.com" => $@"
                        (function() {{
                            var textarea = document.querySelector('textarea') || document.querySelector('[placeholder*='Ask']');
                            if (textarea) {{
                                textarea.value = '{EscapeForJavaScript(prompt)}';
                                textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                return 'success';
                            }}
                            return 'textarea not found';
                        }})();
                    ",
                    // 智谱AI
                    "chatglm.cn" => $@"
                        (function() {{
                            var textarea = document.querySelector('textarea') || document.querySelector('[placeholder*='问题']');
                            if (textarea) {{
                                textarea.value = '{EscapeForJavaScript(prompt)}';
                                textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                return 'success';
                            }}
                            return 'textarea not found';
                        }})();
                    ",
                    // 通义千问
                    "tongyi.aliyun.com" => $@"
                        (function() {{
                            var textarea = document.querySelector('textarea') || document.querySelector('[placeholder*='输入']');
                            if (textarea) {{
                                textarea.value = '{EscapeForJavaScript(prompt)}';
                                textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                return 'success';
                            }}
                            return 'textarea not found';
                        }})();
                    ",
                    // 讯飞星火
                    "xinghuo.xfyun.cn" => $@"
                        (function() {{
                            var textarea = document.querySelector('textarea') || document.querySelector('[placeholder*='输入']') || document.querySelector('[placeholder*='问题']');
                            if (textarea) {{
                                textarea.value = '{EscapeForJavaScript(prompt)}';
                                textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                return 'success';
                            }}
                            return 'textarea not found';
                        }})();
                    ",
                    // 文心一言
                    "yiyan.baidu.com" => $@"
                        (function() {{
                            var textarea = document.querySelector('textarea') || document.querySelector('[placeholder*='输入']') || document.querySelector('[placeholder*='问题']');
                            if (textarea) {{
                                textarea.value = '{EscapeForJavaScript(prompt)}';
                                textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                                textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                return 'success';
                            }}
                            return 'textarea not found';
                        }})();
                    ",
                    _ => "/* unknown host */"
                };

                if (script.StartsWith("/*"))
                {
                    _logger?.LogDebug("跳过未知主机的自动填充: {Host}", host);
                    return;
                }

                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                _logger?.LogDebug("自动填充结果: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "自动填充提示词失败");
            }
        }

        private static string EscapeForJavaScript(string text)
        {
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private void ProviderButton_Click(object? sender, EventArgs e)
        {
            if (sender == btnProviderDeepseek)
                _webView.Source = new Uri("https://chat.deepseek.com/");
            else if (sender == btnProviderDoubao)
                _webView.Source = new Uri("https://www.doubao.com/chat");
            else if (sender == btnProviderZhipu)
                _webView.Source = new Uri("https://chatglm.cn");
            else if (sender == btnProviderQwen)
                _webView.Source = new Uri("https://tongyi.aliyun.com/qianwen");
            else if (sender == btnProviderSpark)
                _webView.Source = new Uri("https://xinghuo.xfyun.cn");
            else if (sender == btnProviderWenxin)
                _webView.Source = new Uri("https://yiyan.baidu.com");
        }

        private void OnTitleChanged(object? sender, object e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke(new Action(() =>
            {
                // 使用页面标题的替代方式（通过 JavaScript 获取或使用文档标题）
                // Text = $"WebView2 浏览器 - {_webView.CoreWebView2.Title ?? "未知"}";
                Text = "WebView2 浏览器";
            }));
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

        private void NavigateToUrl()
        {
            var url = txtUrl.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                if (_webView != null)
                {
                    _webView.Source = new Uri(url);
                }
            }
        }

        private void btnOpenNetdisk_Click(object sender, EventArgs e)
        {
            if (_webView != null)
            {
                _webView.Source = new Uri(BaiduNetdiskUrl);
            }
        }

        private async void btnDownloadNetdisk_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cloudStorageService == null)
                {
                    MessageBox.Show("百度网盘服务未配置或未授权", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_cloudStorageService.IsAuthenticated)
                {
                    MessageBox.Show("请先完成百度网盘授权", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var currentUrl = _webView?.Source?.ToString();
                if (string.IsNullOrEmpty(currentUrl) || !currentUrl.StartsWith("https://pan.baidu.com"))
                {
                    MessageBox.Show("请先浏览到百度网盘文件页面", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var fileInfo = ParseNetdiskUrl(currentUrl);
                if (!fileInfo.HasValue)
                {
                    MessageBox.Show("无法解析网盘文件路径", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 导航到指定URL
        /// </summary>
        public void NavigateToUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            if (_webView != null)
            {
                _webView.Source = new Uri(url);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_webView != null)
                {
                    _webView.NavigationCompleted -= OnNavigationCompleted;
                    // 对应注释掉的 TitleChanged 事件
                    // if (_webView.CoreWebView2 != null)
                    // {
                    //     _webView.CoreWebView2.TitleChanged -= OnTitleChanged;
                    // }
                    _webView.Dispose();
                    _webView = null;
                }
                components?.Dispose();
            }
            base.Dispose(disposing);
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
            comboBoxBookmarks.Size = new Size(150, 25);
            comboBoxBookmarks.SelectedIndexChanged += ComboBoxBookmarks_SelectedIndexChanged;
            // 
            // txtUrl
            // 
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(400, 25);
            txtUrl.Text = "https://www.baidu.com";
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
            btnProviderDeepseek.Click += ProviderButton_Click;
            // 
            // btnProviderDoubao
            // 
            btnProviderDoubao.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderDoubao.ImageTransparentColor = Color.Magenta;
            btnProviderDoubao.Name = "btnProviderDoubao";
            btnProviderDoubao.Size = new Size(36, 22);
            btnProviderDoubao.Text = "豆包";
            btnProviderDoubao.Click += ProviderButton_Click;
            // 
            // btnProviderZhipu
            // 
            btnProviderZhipu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderZhipu.ImageTransparentColor = Color.Magenta;
            btnProviderZhipu.Name = "btnProviderZhipu";
            btnProviderZhipu.Size = new Size(36, 22);
            btnProviderZhipu.Text = "智谱";
            btnProviderZhipu.Click += ProviderButton_Click;
            // 
            // btnProviderQwen
            // 
            btnProviderQwen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderQwen.ImageTransparentColor = Color.Magenta;
            btnProviderQwen.Name = "btnProviderQwen";
            btnProviderQwen.Size = new Size(36, 22);
            btnProviderQwen.Text = "千问";
            btnProviderQwen.Click += ProviderButton_Click;
            // 
            // btnProviderSpark
            // 
            btnProviderSpark.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderSpark.ImageTransparentColor = Color.Magenta;
            btnProviderSpark.Name = "btnProviderSpark";
            btnProviderSpark.Size = new Size(36, 22);
            btnProviderSpark.Text = "讯飞";
            btnProviderSpark.Click += ProviderButton_Click;
            // 
            // btnProviderWenxin
            // 
            btnProviderWenxin.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderWenxin.ImageTransparentColor = Color.Magenta;
            btnProviderWenxin.Name = "btnProviderWenxin";
            btnProviderWenxin.Size = new Size(36, 22);
            btnProviderWenxin.Text = "文心";
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
}