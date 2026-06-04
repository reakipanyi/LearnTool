using System;
using System.IO;
using System.Windows.Forms;
using UnifiedLearningAssistant.Services.Learning;

namespace UnifiedLearningAssistant.Forms
{
    public partial class BrowserForm : Form
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly string _userId = "current_user";

        public BrowserForm(IContentLoaderService contentLoaderService)
        {
            _contentLoaderService = contentLoaderService;
            
            InitializeComponent();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            MessageBox.Show("浏览器功能需要CefSharp库支持，请先安装相关依赖包。", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            MessageBox.Show("浏览器功能需要CefSharp库支持，请先安装相关依赖包。", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveAsPdf_Click(object sender, EventArgs e)
        {
            MessageBox.Show("浏览器功能需要CefSharp库支持，请先安装相关依赖包。", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnGo.PerformClick();
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
        private ToolStripButton btnExtract;
        private ToolStripButton btnSaveAsPdf;
        private Panel panelBrowser;
        private Label lblInfo;

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
            this.lblInfo = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            this.panelBrowser.SuspendLayout();
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
            this.btnBack.Text = "后退";
            this.btnBack.Size = new System.Drawing.Size(23, 22);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

            this.btnForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnForward.Text = "前进";
            this.btnForward.Size = new System.Drawing.Size(23, 22);
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);

            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.Size = new System.Drawing.Size(23, 22);
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.txtUrl.Size = new System.Drawing.Size(400, 25);
            this.txtUrl.Text = "https://www.baidu.com";
            this.txtUrl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtUrl_KeyDown);

            this.btnGo.Text = "跳转";
            this.btnGo.Size = new System.Drawing.Size(46, 22);
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);

            this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);

            this.btnExtract.Text = "提取内容";
            this.btnExtract.Size = new System.Drawing.Size(68, 22);
            this.btnExtract.Click += new System.EventHandler(this.btnExtract_Click);

            this.btnSaveAsPdf.Text = "保存PDF";
            this.btnSaveAsPdf.Size = new System.Drawing.Size(68, 22);
            this.btnSaveAsPdf.Click += new System.EventHandler(this.btnSaveAsPdf_Click);

            this.panelBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBrowser.Location = new System.Drawing.Point(0, 25);
            this.panelBrowser.Size = new System.Drawing.Size(800, 425);
            this.panelBrowser.Controls.Add(this.lblInfo);

            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(20, 20);
            this.lblInfo.Text = "浏览器功能需要CefSharp库支持。\n请在项目中添加CefSharp.WinForms包以启用此功能。";

            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelBrowser);
            this.Controls.Add(this.toolStrip);
            this.Text = "学习浏览器";

            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.panelBrowser.ResumeLayout(false);
            this.panelBrowser.PerformLayout();
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