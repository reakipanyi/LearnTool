using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms
{
    public partial class BrowserForm : Form
    {
        private readonly IContentLoaderService _contentLoaderService;
        private ChromiumWebBrowser? _browser;

        public BrowserForm(IContentLoaderService contentLoaderService)
        {
            _contentLoaderService = contentLoaderService;
            InitializeComponent();
            InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            try
            {
                if (!Cef.IsInitialized && !Cef.Initialize())
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

                panelBrowser.Controls.Add(_browser);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化浏览器失败: {ex.Message}\n\n详细信息: {ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    }
                }
            }));
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
                if (_browser != null)
                {
                    try
                    {
                        _browser.LoadingStateChanged -= OnLoadingStateChanged;
                        _browser.TitleChanged -= OnTitleChanged;
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
            this.btnSaveAsPdf});
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
