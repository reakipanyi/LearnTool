using CefSharp;
using CefSharp.WinForms;
using LearningAssistant.Common;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    public partial class BrowserForm : Form
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly ICloudStorageService? _cloudStorageService;
        private readonly ILogger<BrowserForm>? _logger;
        private ChromiumWebBrowser? _browser;
        private const string BaiduNetdiskUrl = "https://pan.baidu.com";
        private const string CookieFileName = "browser_cookies.dat";

        public BrowserForm(IContentLoaderService contentLoaderService,
                          ICloudStorageService? cloudStorageService = null,
                          ILogger<BrowserForm>? logger = null)
        {
            _contentLoaderService = contentLoaderService;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            InitializeComponent();
            InitializeBrowser();
            LoadCookies();

        }

        private void InitializeBrowser()
        {
            try
            {
                var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheDir = Path.Combine(appDataDir, "LearningAssistant", "browser_cache");
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }
                var settings = new CefSharp.WinForms.CefSettings
                {
                    CachePath = cacheDir,
                    PersistSessionCookies = true,
                    LogSeverity = LogSeverity.Error,
                    LogFile = Path.Combine(cacheDir, "cef.log")
                };

                if (Cef.IsInitialized != true && !Cef.Initialize(settings))
                {
                    MessageBox.Show("无法初始化 CefSharp 浏览器引擎", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _browser = new ChromiumWebBrowser("https://www.baidu.com")
                {
                    Dock = DockStyle.Fill
                };

                // 注册事件处理器
                _browser.LoadingStateChanged += OnLoadingStateChanged;
                _browser.TitleChanged += OnTitleChanged;
                _browser.FrameLoadEnd += OnFrameLoadEnd;

                panelBrowser.Controls.Add(_browser);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化浏览器失败: {ex.Message}\n\n详细信息: {ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCookies()
        {
            try
            {
                var cookiePath = GetCookieFilePath();
                if (File.Exists(cookiePath))
                {
                    _logger?.LogInformation("已加载保存的Cookie（自动由CefSharp管理）");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载Cookie失败，将使用新会话");
            }
        }

        private void SaveCookies()
        {
            try
            {
                // 让CefSharp自动保存Cookie到CachePath
                _logger?.LogInformation("Cookie将由CefSharp自动保存到缓存目录");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存Cookie失败");
            }
        }

        private string GetCookieFilePath()
        {
            return Path.Combine(Paths.DataDirectory, CookieFileName);
        }

        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke(new Action(() =>
            {
                if (_browser != null)
                {
                    btnBack.Enabled = _browser.CanGoBack;
                    btnForward.Enabled = _browser.CanGoForward;
                    btnRefresh.Enabled = !e.IsLoading;

                    if (!e.IsLoading)
                    {
                        txtUrl.Text = _browser.Address;

                        // 检测是否在百度网盘页面
                        bool isNetdiskPage = _browser.Address.StartsWith("https://pan.baidu.com");
                        btnOpenNetdisk.Visible = !isNetdiskPage;
                        btnDownloadNetdisk.Visible = isNetdiskPage && _cloudStorageService != null && _cloudStorageService.IsAuthenticated;
                    }
                }
            }));
        }

        private void OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            // 页面加载完成后保存Cookie（特别是百度网盘登录后）
            if (e.Url.StartsWith("https://pan.baidu.com"))
            {
                SaveCookies();
            }
        }

        private void OnTitleChanged(object sender, TitleChangedEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            BeginInvoke(new Action(() =>
            {
                Text = $"学习浏览器 - {e.Title}";
            }));
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            NavigateToUrl();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (_browser?.CanGoBack == true)
            {
                _browser.Back();
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (_browser?.CanGoForward == true)
            {
                _browser.Forward();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _browser?.Reload();
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

                if (_browser != null)
                {
                    _browser.Load(url);
                }
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            try
            {
                if (_browser != null)
                {
                    MessageBox.Show("内容提取功能开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"提取内容失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenNetdisk_Click(object sender, EventArgs e)
        {
            try
            {
                if (_browser != null)
                {
                    _browser.Load(BaiduNetdiskUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开百度网盘失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // 获取当前页面URL中的文件路径信息
                var currentUrl = _browser?.Address;
                if (string.IsNullOrEmpty(currentUrl) || !currentUrl.StartsWith("https://pan.baidu.com"))
                {
                    MessageBox.Show("请先浏览到百度网盘文件页面", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 解析URL获取文件路径
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
                    // 显示进度对话框
                    using var progressForm = new ProgressForm();
                    progressForm.Show();

                    bool success = await _cloudStorageService.DownloadFileAsync(
                        cloudPath,
                        saveDialog.FileName,
                        (progress) =>
                        {
                            progressForm.UpdateProgress(progress);
                        });

                    progressForm.Close();

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

                // 尝试从URL参数获取文件路径
                var path = query["path"];
                if (!string.IsNullOrEmpty(path))
                {
                    return (path, Path.GetFileName(path));
                }

                // 尝试从URL路径获取
                var segments = uri.Segments;
                if (segments.Length >= 3)
                {
                    // 格式: /s/xxxx 或 /share/xxxx
                    if (segments[1].Equals("s/", StringComparison.OrdinalIgnoreCase))
                    {
                        // 分享链接，需要特殊处理
                        var shareId = segments[2].TrimEnd('/');
                        return ($"/share/{shareId}", shareId);
                    }
                }

                // 默认返回根目录
                return ("/", "root");
            }
            catch
            {
                return null;
            }
        }

        private async void btnSaveAsPdf_Click(object sender, EventArgs e)
        {
            try
            {
                if (_browser != null)
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "PDF 文件 (*.pdf)|*.pdf",
                        DefaultExt = "pdf",
                        Title = "保存为 PDF"
                    };

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        var success = await _browser.PrintToPdfAsync(saveDialog.FileName);
                        if (success)
                        {
                            MessageBox.Show("PDF 保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("PDF 保存失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存 PDF 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 关闭前保存Cookie
                SaveCookies();
                try
                {
                    if (Cef.IsInitialized == true)
                    {
                        Cef.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"关闭 CefSharp 失败: {ex.Message}");
                }
                if (_browser != null)
                {
                    try
                    {
                        _browser.LoadingStateChanged -= OnLoadingStateChanged;
                        _browser.TitleChanged -= OnTitleChanged;
                        _browser.FrameLoadEnd -= OnFrameLoadEnd;
                        _browser.Dispose();
                    }
                    catch { }
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
        private ToolStripButton btnExtract;
        private ToolStripButton btnSaveAsPdf;
        private Panel panelBrowser;
        private ToolStripButton btnOpenNetdisk;
        private ToolStripButton btnDownloadNetdisk;

        private void InitializeComponent()
        {
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnBack = new System.Windows.Forms.ToolStripButton();
            this.btnForward = new System.Windows.Forms.ToolStripButton();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.txtUrl = new System.Windows.Forms.ToolStripTextBox();
            this.btnGo = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.btnExtract = new System.Windows.Forms.ToolStripButton();
            this.btnSaveAsPdf = new System.Windows.Forms.ToolStripButton();
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
            this.btnExtract,
            this.btnSaveAsPdf,
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
            // btnExtract
            // 
            this.btnExtract.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnExtract.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(60, 22);
            this.btnExtract.Text = "提取内容";
            this.btnExtract.Click += new System.EventHandler(this.btnExtract_Click);
            // 
            // btnSaveAsPdf
            // 
            this.btnSaveAsPdf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSaveAsPdf.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSaveAsPdf.Name = "btnSaveAsPdf";
            this.btnSaveAsPdf.Size = new System.Drawing.Size(60, 22);
            this.btnSaveAsPdf.Text = "保存PDF";
            this.btnSaveAsPdf.Click += new System.EventHandler(this.btnSaveAsPdf_Click);
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
            // BrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelBrowser);
            this.Controls.Add(this.toolStrip);
            this.Name = "BrowserForm";
            this.Text = "学习浏览器";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private void panelBrowser_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
