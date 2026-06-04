using System;
using System.IO;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using UnifiedLearningAssistant.Services.Learning;

namespace UnifiedLearningAssistant.Forms
{
    public partial class BrowserForm : Form
    {
        private readonly ChromiumWebBrowser _browser;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly string _userId = "current_user";

        public BrowserForm(IContentLoaderService contentLoaderService)
        {
            InitializeComponent();
            _contentLoaderService = contentLoaderService;
            
            InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            Cef.Initialize(new CefSettings());
            _browser = new ChromiumWebBrowser("https://www.baidu.com")
            {
                Dock = DockStyle.Fill,
            };
            
            _browser.LoadingStateChanged += Browser_LoadingStateChanged;
            _browser.FrameLoadEnd += Browser_FrameLoadEnd;
            
            panelBrowser.Controls.Add(_browser);
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                txtUrl.Text = _browser.Address;
            }
        }

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            btnExtract.Enabled = true;
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            var url = txtUrl.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                _browser.Load(url);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (_browser.CanGoBack)
            {
                _browser.Back();
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (_browser.CanGoForward)
            {
                _browser.Forward();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _browser.Refresh();
        }

        private async void btnExtract_Click(object sender, EventArgs e)
        {
            try
            {
                var html = await _browser.GetSourceAsync();
                var title = await _browser.GetTitleAsync();
                
                var content = new
                {
                    Title = title,
                    Url = _browser.Address,
                    HtmlContent = html,
                    CreatedAt = DateTime.Now
                };
                
                var tempPath = Path.GetTempFileName() + ".html";
                File.WriteAllText(tempPath, html);
                
                await _contentLoaderService.ImportFromFileAsync(_userId, tempPath);
                
                MessageBox.Show($"网页内容已导入学习库\n标题: {title}", "导入成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveAsPdf_Click(object sender, EventArgs e)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), $"webpage_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                _browser.PrintToPdfAsync(path).ContinueWith(task =>
                {
                    if (task.Result)
                    {
                        MessageBox.Show($"网页已保存为PDF: {path}", "保存成功", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("保存失败", "错误", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnGo.PerformClick();
            }
        }

        private void BrowserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _browser.Dispose();
            Cef.Shutdown();
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
            this.components = new System.ComponentModel.Container();
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
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;

            this.btnBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnBack.Image = System.Drawing.SystemIcons.ArrowLeft.ToBitmap();
            this.btnBack.Size = new System.Drawing.Size(23, 22);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

            this.btnForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnForward.Image = System.Drawing.SystemIcons.ArrowRight.ToBitmap();
            this.btnForward.Size = new System.Drawing.Size(23, 22);
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);

            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnRefresh.Image = System.Drawing.SystemIcons.Refresh.ToBitmap();
            this.btnRefresh.Size = new System.Drawing.Size(23, 22);
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.txtUrl.Size = new System.Drawing.Size(400, 25);
            this.txtUrl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtUrl_KeyDown);

            this.btnGo.Text = "跳转";
            this.btnGo.Size = new System.Drawing.Size(46, 22);
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);

            this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);

            this.btnExtract.Text = "提取内容";
            this.btnExtract.Size = new System.Drawing.Size(68, 22);
            this.btnExtract.Enabled = false;
            this.btnExtract.Click += new System.EventHandler(this.btnExtract_Click);

            this.btnSaveAsPdf.Text = "保存PDF";
            this.btnSaveAsPdf.Size = new System.Drawing.Size(68, 22);
            this.btnSaveAsPdf.Click += new System.EventHandler(this.btnSaveAsPdf_Click);

            this.panelBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBrowser.Location = new System.Drawing.Point(0, 25);
            this.panelBrowser.Size = new System.Drawing.Size(800, 425);

            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelBrowser);
            this.Controls.Add(this.toolStrip);
            this.Text = "学习浏览器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BrowserForm_FormClosing);

            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}