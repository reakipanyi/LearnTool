using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using LearningAssistant.Services.Cloud;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LearningAssistant.Forms
{
    public partial class WebView2BrowserForm : Form
    {
        private readonly ICloudStorageService? _cloudStorageService;
        private readonly ILogger<WebView2BrowserForm>? _logger;
        private WebView2? _webView;
        private const string BaiduNetdiskUrl = "https://pan.baidu.com";
        private bool _isWebViewReady = false;

        public WebView2BrowserForm(ICloudStorageService? cloudStorageService = null,
                                   ILogger<WebView2BrowserForm>? logger = null)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            InitializeComponent();
            Load += WebView2BrowserForm_Load;
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
                        _webView.CoreWebView2.TitleChanged += OnTitleChanged;

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

                    bool isNetdiskPage = _webView.Source?.ToString()?.StartsWith("https://pan.baidu.com") ?? false;
                    btnOpenNetdisk.Visible = !isNetdiskPage;
                    btnDownloadNetdisk.Visible = isNetdiskPage && _cloudStorageService != null && _cloudStorageService.IsAuthenticated;
                }
            }));
        }

        private void OnTitleChanged(object? sender, object e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke(new Action(() =>
            {
                if (_webView?.CoreWebView2 != null)
                {
                    Text = $"WebView2 浏览器 - {_webView.CoreWebView2.Title ?? "未知"}";
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_webView != null)
                {
                    _webView.NavigationCompleted -= OnNavigationCompleted;
                    if (_webView.CoreWebView2 != null)
                    {
                        _webView.CoreWebView2.TitleChanged -= OnTitleChanged;
                    }
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

        private void InitializeComponent()
        {
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnBack = new System.Windows.Forms.ToolStripButton();
            this.btnForward = new System.Windows.Forms.ToolStripButton();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.txtUrl = new System.Windows.Forms.ToolStripTextBox();
            this.btnGo = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.btnOpenNetdisk = new System.Windows.Forms.ToolStripButton();
            this.btnDownloadNetdisk = new System.Windows.Forms.ToolStripButton();
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
            this.toolStripSeparator,
            this.btnOpenNetdisk,
            this.btnDownloadNetdisk});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
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
            this.txtUrl.Size = new System.Drawing.Size(400, 25);
            this.txtUrl.Text = "https://www.baidu.com";
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
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // btnOpenNetdisk
            // 
            this.btnOpenNetdisk.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnOpenNetdisk.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpenNetdisk.Name = "btnOpenNetdisk";
            this.btnOpenNetdisk.Size = new System.Drawing.Size(60, 22);
            this.btnOpenNetdisk.Text = "百度网盘";
            this.btnOpenNetdisk.Click += new System.EventHandler(this.btnOpenNetdisk_Click);
            // 
            // btnDownloadNetdisk
            // 
            this.btnDownloadNetdisk.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnDownloadNetdisk.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDownloadNetdisk.Name = "btnDownloadNetdisk";
            this.btnDownloadNetdisk.Size = new System.Drawing.Size(60, 22);
            this.btnDownloadNetdisk.Text = "下载文件";
            this.btnDownloadNetdisk.Visible = false;
            this.btnDownloadNetdisk.Click += new System.EventHandler(this.btnDownloadNetdisk_Click);
            // 
            // panelBrowser
            // 
            this.panelBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBrowser.Location = new System.Drawing.Point(0, 25);
            this.panelBrowser.Name = "panelBrowser";
            this.panelBrowser.Size = new System.Drawing.Size(800, 425);
            this.panelBrowser.TabIndex = 1;
            // 
            // WebView2BrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelBrowser);
            this.Controls.Add(this.toolStrip);
            this.Name = "WebView2BrowserForm";
            this.Text = "WebView2 浏览器";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}