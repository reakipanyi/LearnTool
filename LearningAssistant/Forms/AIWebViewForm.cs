using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;

namespace LearningAssistant.Forms
{
    public partial class AIWebViewForm : Form
    {
        private readonly ILogger<AIWebViewForm>? _logger;
        private WebView2? _webView;
        private readonly string _initialUrl;
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

        public AIWebViewForm(ILogger<AIWebViewForm>? logger = null, string? initialUrl = null, string? initialPrompt = null)
        {
            _logger = logger;
            _initialPrompt = initialPrompt;
            if (!string.IsNullOrEmpty(initialUrl))
            {
                _initialUrl = initialUrl;
            }
            else if (AiConfig.Providers.TryGetValue("doubao", out var defaultProvider))
            {
                _initialUrl = defaultProvider.WebViewUrl;
            }
            else
            {
                _initialUrl = "https://www.doubao.com/chat";
            }
            InitializeComponent();
            Load += AIWebViewForm_Load;
        }

        private async void AIWebViewForm_Load(object? sender, EventArgs e)
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
                    var cacheDir = Path.Combine(appDataDir, "LearningAssistant", "ai_webview_cache");
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
                        _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;

                        _webView.Source = new Uri(_initialUrl);

                        _webView.NavigationCompleted += OnNavigationCompleted;

                        panelBrowser.Controls.Add(_webView);
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
                    _logger?.LogError(ex, "WebView2 初始化失败 (尝试 {RetryCount}/{MaxRetryCount})", retryCount, maxRetryCount);

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

                    string? currentHost = _webView.Source?.Host;
                    Text = "AI 助手";

                    // 重置所有平台按钮
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


                    // 高亮当前平台按钮（使用Host精确匹配）
                    if (currentHost == "chat.deepseek.com")
                    {
                        btnProviderDeepseek.BackColor = Color.LightBlue;
                        btnProviderDeepseek.Enabled = false;
                        Text = "🤖 DeepSeek";
                    }
                    else if (currentHost == "www.doubao.com")
                    {
                        btnProviderDoubao.BackColor = Color.LightBlue;
                        btnProviderDoubao.Enabled = false;
                        Text = "🤖 豆包 (Doubao)";
                    }
                    else if (currentHost == "chat.deepseek.com")
                    {
                        btnProviderDeepseek.BackColor = Color.LightBlue;
                        btnProviderDeepseek.Enabled = false;
                        Text = "🤖 DeepSeek";
                    }

                    else if (currentHost == "chatglm.cn")
                    {
                        btnProviderZhipu.BackColor = Color.LightBlue;
                        btnProviderZhipu.Enabled = false;
                        Text = "🤖 智谱AI (Zhipu/GLM)";
                    }
                    else if (currentHost == "tongyi.aliyun.com")
                    {
                        btnProviderQwen.BackColor = Color.LightBlue;
                        btnProviderQwen.Enabled = false;
                        Text = "🤖 通义千问 (Qwen/DashScope)";
                    }
                    else if (currentHost == "xinghuo.xfyun.cn")
                    {
                        btnProviderSpark.BackColor = Color.LightBlue;
                        btnProviderSpark.Enabled = false;
                        Text = "🤖 讯飞星火 (Spark)";
                    }
                    else if (currentHost == "yiyan.baidu.com")
                    {
                        btnProviderWenxin.BackColor = Color.LightBlue;
                        btnProviderWenxin.Enabled = false;
                        Text = "🤖 文心一言 (ERNIE)";
                    }

                    // 自动填充提示词
                    if (!string.IsNullOrEmpty(_initialPrompt))
                    {
                        await FillPromptAsync(_initialPrompt, currentHost);
                        _initialPrompt = null; // 只填充一次
                    }
                }
            }));
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private ToolStrip toolStrip;
        private ToolStripButton btnBack;
        private ToolStripButton btnForward;
        private ToolStripButton btnRefresh;
        private ToolStripTextBox txtUrl;
        private ToolStripButton btnGo;
        private ToolStripSeparator toolStripSeparatorProvider;
        private ToolStripButton btnProviderDeepseek;
        private ToolStripButton btnProviderDoubao;
        private ToolStripButton btnProviderZhipu;
        private ToolStripButton btnProviderQwen;
        private ToolStripButton btnProviderSpark;
        private ToolStripButton btnProviderWenxin;

        private Panel panelBrowser;

        private void InitializeComponent()
        {
            toolStrip = new ToolStrip();
            btnBack = new ToolStripButton();
            btnForward = new ToolStripButton();
            btnRefresh = new ToolStripButton();
            txtUrl = new ToolStripTextBox();
            btnGo = new ToolStripButton();
            toolStripSeparatorProvider = new ToolStripSeparator();
            btnProviderDeepseek = new ToolStripButton();
            btnProviderDoubao = new ToolStripButton();
            btnProviderZhipu = new ToolStripButton();
            btnProviderQwen = new ToolStripButton();
            btnProviderSpark = new ToolStripButton();
            btnProviderWenxin = new ToolStripButton();
            panelBrowser = new Panel();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.Items.AddRange(new ToolStripItem[] { btnBack, btnForward, btnRefresh, txtUrl, btnGo, toolStripSeparatorProvider, btnProviderDeepseek, btnProviderDoubao, btnProviderZhipu, btnProviderQwen, btnProviderSpark, btnProviderWenxin });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1548, 25);
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
            // txtUrl
            // 
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(500, 25);
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
            btnProviderDoubao.Size = new Size(95, 22);
            btnProviderDoubao.Text = "豆包 (Doubao)";
            btnProviderDoubao.Click += ProviderButton_Click;
            // 
            // btnProviderZhipu
            // 
            btnProviderZhipu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderZhipu.ImageTransparentColor = Color.Magenta;
            btnProviderZhipu.Name = "btnProviderZhipu";
            btnProviderZhipu.Size = new Size(124, 22);
            btnProviderZhipu.Text = "智谱AI (Zhipu/GLM)";
            btnProviderZhipu.Click += ProviderButton_Click;
            // 
            // btnProviderQwen
            // 
            btnProviderQwen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderQwen.ImageTransparentColor = Color.Magenta;
            btnProviderQwen.Name = "btnProviderQwen";
            btnProviderQwen.Size = new Size(175, 22);
            btnProviderQwen.Text = "通义千问 (Qwen/DashScope)";
            btnProviderQwen.Click += ProviderButton_Click;
            // 
            // btnProviderSpark
            // 
            btnProviderSpark.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderSpark.ImageTransparentColor = Color.Magenta;
            btnProviderSpark.Name = "btnProviderSpark";
            btnProviderSpark.Size = new Size(100, 22);
            btnProviderSpark.Text = "讯飞星火 (Spark)";
            btnProviderSpark.Click += ProviderButton_Click;
            // 
            // btnProviderWenxin
            // 
            btnProviderWenxin.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnProviderWenxin.ImageTransparentColor = Color.Magenta;
            btnProviderWenxin.Name = "btnProviderWenxin";
            btnProviderWenxin.Size = new Size(100, 22);
            btnProviderWenxin.Text = "文心一言 (ERNIE)";
            btnProviderWenxin.Click += ProviderButton_Click;
            // 
            // panelBrowser
            // 
            panelBrowser.Dock = DockStyle.Fill;
            panelBrowser.Location = new Point(0, 25);
            panelBrowser.Name = "panelBrowser";
            panelBrowser.Size = new Size(1548, 768);
            panelBrowser.TabIndex = 1;
            // 
            // AIWebViewForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1548, 793);
            Controls.Add(panelBrowser);
            Controls.Add(toolStrip);
            Name = "AIWebViewForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🤖 AI 助手";
            WindowState = FormWindowState.Maximized;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
