using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LearningAssistant.Forms
{
    public partial class AIWebViewForm : Form
    {
        private readonly ILogger<AIWebViewForm>? _logger;
        private WebView2? _webView;
        private bool _isWebViewReady = false;

        private const string DoubaoUrl = "https://www.doubao.com/chat";
        private const string DeepseekUrl = "https://chat.deepseek.com/";

        public AIWebViewForm(ILogger<AIWebViewForm>? logger = null)
        {
            _logger = logger;
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
                        _webView.CoreWebView2.Settings.DefaultZoomFactor = 1.0;

                        _webView.Source = new Uri(DoubaoUrl);

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

            BeginInvoke(new Action(() =>
            {
                if (_webView != null)
                {
                    txtUrl.Text = _webView.Source?.ToString() ?? string.Empty;
                    btnBack.Enabled = _webView.CanGoBack;
                    btnForward.Enabled = _webView.CanGoForward;

                    bool isDoubao = _webView.Source?.ToString()?.StartsWith("https://www.doubao.com") ?? false;
                    bool isDeepseek = _webView.Source?.ToString()?.StartsWith("https://chat.deepseek.com") ?? false;

                    btnDoubao.Enabled = !isDoubao;
                    btnDeepseek.Enabled = !isDeepseek;

                    if (isDoubao)
                    {
                        Text = "🤖 豆包 AI";
                        btnDoubao.BackColor = Color.LightBlue;
                        btnDeepseek.BackColor = SystemColors.Control;
                    }
                    else if (isDeepseek)
                    {
                        Text = "🤖 DeepSeek AI";
                        btnDeepseek.BackColor = Color.LightBlue;
                        btnDoubao.BackColor = SystemColors.Control;
                    }
                    else
                    {
                        Text = "AI 助手";
                        btnDoubao.BackColor = SystemColors.Control;
                        btnDeepseek.BackColor = SystemColors.Control;
                    }
                }
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

        private void btnDoubao_Click(object sender, EventArgs e)
        {
            if (_webView != null)
            {
                _webView.Source = new Uri(DoubaoUrl);
            }
        }

        private void btnDeepseek_Click(object sender, EventArgs e)
        {
            if (_webView != null)
            {
                _webView.Source = new Uri(DeepseekUrl);
            }
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
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton btnDoubao;
        private ToolStripButton btnDeepseek;
        private Panel panelBrowser;

        private void InitializeComponent()
        {
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnBack = new System.Windows.Forms.ToolStripButton();
            this.btnForward = new System.Windows.Forms.ToolStripButton();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.txtUrl = new System.Windows.Forms.ToolStripTextBox();
            this.btnGo = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnDoubao = new System.Windows.Forms.ToolStripButton();
            this.btnDeepseek = new System.Windows.Forms.ToolStripButton();
            this.panelBrowser = new System.Windows.Forms.Panel();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnBack,
            this.btnForward,
            this.btnRefresh,
            this.txtUrl,
            this.btnGo,
            this.toolStripSeparator1,
            this.btnDoubao,
            this.btnDeepseek});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1024, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // btnBack
            // 
            this.btnBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnBack.Enabled = false;
            this.btnBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(36, 22);
            this.btnBack.Text = "后退";
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // btnForward
            // 
            this.btnForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnForward.Enabled = false;
            this.btnForward.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(36, 22);
            this.btnForward.Text = "前进";
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(36, 22);
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // txtUrl
            // 
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(500, 25);
            this.txtUrl.Text = "https://www.doubao.com/chat";
            this.txtUrl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtUrl_KeyDown);
            // 
            // btnGo
            // 
            this.btnGo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnGo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(36, 22);
            this.btnGo.Text = "跳转";
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnDoubao
            // 
            this.btnDoubao.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnDoubao.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDoubao.Name = "btnDoubao";
            this.btnDoubao.Size = new System.Drawing.Size(60, 22);
            this.btnDoubao.Text = "豆包";
            this.btnDoubao.Click += new System.EventHandler(this.btnDoubao_Click);
            // 
            // btnDeepseek
            // 
            this.btnDeepseek.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnDeepseek.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDeepseek.Name = "btnDeepseek";
            this.btnDeepseek.Size = new System.Drawing.Size(72, 22);
            this.btnDeepseek.Text = "DeepSeek";
            this.btnDeepseek.Click += new System.EventHandler(this.btnDeepseek_Click);
            // 
            // panelBrowser
            // 
            this.panelBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBrowser.Location = new System.Drawing.Point(0, 25);
            this.panelBrowser.Name = "panelBrowser";
            this.panelBrowser.Size = new System.Drawing.Size(1024, 675);
            this.panelBrowser.TabIndex = 1;
            // 
            // AIWebViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 700);
            this.Controls.Add(this.panelBrowser);
            this.Controls.Add(this.toolStrip);
            this.Name = "AIWebViewForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "🤖 豆包 AI";
            this.WindowState = FormWindowState.Maximized;
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}