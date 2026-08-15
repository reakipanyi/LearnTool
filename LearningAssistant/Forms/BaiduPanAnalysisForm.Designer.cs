using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 百度网盘 AI 分析窗体（设计器文件）
    /// </summary>
    partial class BaiduPanAnalysisForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService?.UnregisterThemeable(this);
                _cts?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            mainPanel = new TableLayoutPanel();
            topPanel = new FlowLayoutPanel();
            lblPath = new Label();
            txtPath = new TextBox();
            lblDepth = new Label();
            cmbDepth = new ComboBox();
            chkDetectDuplicates = new CheckBox();
            chkUseCache = new CheckBox();
            btnStartAnalysis = new Button();
            btnCancel = new Button();
            btnExecute = new Button();
            contentPanel = new SplitContainer();
            leftPanel = new Panel();
            rightPanel = new Panel();
            lstRecommendations = new ListView();
            txtSummary = new TextBox();
            txtLog = new TextBox();
            mainPanel.SuspendLayout();
            topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)contentPanel).BeginInit();
            contentPanel.Panel1.SuspendLayout();
            contentPanel.Panel2.SuspendLayout();
            contentPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainPanel
            // 
            mainPanel.ColumnCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(contentPanel, 0, 1);
            mainPanel.Controls.Add(txtLog, 0, 2);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 0);
            mainPanel.Margin = new Padding(0);
            mainPanel.Name = "mainPanel";
            mainPanel.Padding = new Padding(8);
            mainPanel.RowCount = 3;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            mainPanel.Size = new Size(1034, 681);
            mainPanel.TabIndex = 0;
            // 
            // topPanel
            // 
            topPanel.Controls.Add(lblPath);
            topPanel.Controls.Add(txtPath);
            topPanel.Controls.Add(lblDepth);
            topPanel.Controls.Add(cmbDepth);
            topPanel.Controls.Add(chkDetectDuplicates);
            topPanel.Controls.Add(chkUseCache);
            topPanel.Controls.Add(btnStartAnalysis);
            topPanel.Controls.Add(btnCancel);
            topPanel.Controls.Add(btnExecute);
            topPanel.Dock = DockStyle.Fill;
            topPanel.Location = new Point(11, 11);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(4, 6, 4, 6);
            topPanel.Size = new Size(1012, 38);
            topPanel.TabIndex = 0;
            // 
            // lblPath
            // 
            lblPath.Anchor = AnchorStyles.Left;
            lblPath.AutoSize = true;
            lblPath.Location = new Point(8, 16);
            lblPath.Margin = new Padding(4, 8, 2, 8);
            lblPath.Name = "lblPath";
            lblPath.Size = new Size(44, 17);
            lblPath.TabIndex = 0;
            lblPath.Text = "目录：";
            // 
            // txtPath
            // 
            txtPath.Location = new Point(56, 12);
            txtPath.Margin = new Padding(2, 6, 8, 6);
            txtPath.Name = "txtPath";
            txtPath.ReadOnly = true;
            txtPath.Size = new Size(260, 23);
            txtPath.TabIndex = 1;
            // 
            // lblDepth
            // 
            lblDepth.Anchor = AnchorStyles.Left;
            lblDepth.AutoSize = true;
            lblDepth.Location = new Point(328, 16);
            lblDepth.Margin = new Padding(4, 8, 2, 8);
            lblDepth.Name = "lblDepth";
            lblDepth.Size = new Size(44, 17);
            lblDepth.TabIndex = 2;
            lblDepth.Text = "深度：";
            // 
            // cmbDepth
            // 
            cmbDepth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDepth.Items.AddRange(new object[] { 1, 2, 3, 5, 0 });
            cmbDepth.SelectedIndex = 1; // 默认深度 2
            cmbDepth.Location = new Point(376, 12);
            cmbDepth.Margin = new Padding(2, 6, 8, 6);
            cmbDepth.Name = "cmbDepth";
            cmbDepth.Size = new Size(70, 25);
            cmbDepth.TabIndex = 3;
            // 
            // chkDetectDuplicates
            // 
            chkDetectDuplicates.AutoSize = true;
            chkDetectDuplicates.Checked = true;
            chkDetectDuplicates.CheckState = CheckState.Checked;
            chkDetectDuplicates.Location = new Point(458, 14);
            chkDetectDuplicates.Margin = new Padding(4, 8, 4, 8);
            chkDetectDuplicates.Name = "chkDetectDuplicates";
            chkDetectDuplicates.Size = new Size(75, 21);
            chkDetectDuplicates.TabIndex = 4;
            chkDetectDuplicates.Text = "重复检测";
            // 
            // chkUseCache
            // 
            chkUseCache.AutoSize = true;
            chkUseCache.Checked = true;
            chkUseCache.CheckState = CheckState.Checked;
            chkUseCache.Location = new Point(541, 14);
            chkUseCache.Margin = new Padding(4, 8, 8, 8);
            chkUseCache.Name = "chkUseCache";
            chkUseCache.Size = new Size(75, 21);
            chkUseCache.TabIndex = 5;
            chkUseCache.Text = "使用缓存";
            // 
            // btnStartAnalysis
            // 
            btnStartAnalysis.Location = new Point(632, 10);
            btnStartAnalysis.Margin = new Padding(8, 4, 4, 4);
            btnStartAnalysis.Name = "btnStartAnalysis";
            btnStartAnalysis.Size = new Size(110, 23);
            btnStartAnalysis.TabIndex = 6;
            btnStartAnalysis.Text = "🚀 开始分析";
            btnStartAnalysis.Click += btnStartAnalysis_Click;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Location = new Point(750, 10);
            btnCancel.Margin = new Padding(4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(70, 23);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "取消";
            btnCancel.Click += btnCancel_Click;
            // 
            // btnExecute
            // 
            btnExecute.Location = new Point(828, 10);
            btnExecute.Margin = new Padding(4);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new Size(140, 23);
            btnExecute.TabIndex = 8;
            btnExecute.Text = "✅ 执行选中操作";
            btnExecute.Click += btnExecute_Click;
            // 
            // contentPanel
            // 
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(11, 55);
            contentPanel.Name = "contentPanel";
            contentPanel.Size = new Size(1012, 475);
            // 
            // contentPanel.Panel1
            // 
            contentPanel.Panel1.Controls.Add(leftPanel);
            contentPanel.Panel1MinSize = 260;
            // 
            // contentPanel.Panel2
            // 
            // 注意：SplitterDistance 必须在 Panel2MinSize 之前设置，
            // 否则在 WinForms 应用 Panel2MinSize 时会基于默认 SplitterDistance(50) 校验，
            // 导致 50 < Panel1MinSize(260) 抛出 SplitterDistance 越界异常。
            contentPanel.Panel2.Controls.Add(rightPanel);
            contentPanel.SplitterDistance = 508;
            contentPanel.Panel2MinSize = 500;
            contentPanel.TabIndex = 1;
            // 
            // leftPanel
            // 
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Padding = new Padding(0, 4, 6, 0);
            leftPanel.Size = new Size(508, 475);
            leftPanel.TabIndex = 0;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(lstRecommendations);
            rightPanel.Controls.Add(txtSummary);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(0, 0);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(6, 4, 0, 0);
            rightPanel.Size = new Size(500, 475);
            rightPanel.TabIndex = 0;
            // 
            // lstRecommendations
            // 
            lstRecommendations.CheckBoxes = true;
            lstRecommendations.Dock = DockStyle.Fill;
            lstRecommendations.FullRowSelect = true;
            lstRecommendations.GridLines = true;
            lstRecommendations.Location = new Point(6, 68);
            lstRecommendations.Name = "lstRecommendations";
            lstRecommendations.Size = new Size(494, 407);
            lstRecommendations.TabIndex = 0;
            lstRecommendations.UseCompatibleStateImageBehavior = false;
            lstRecommendations.View = View.Details;
            // 
            // txtSummary
            // 
            txtSummary.BackColor = SystemColors.Info;
            txtSummary.Dock = DockStyle.Top;
            txtSummary.Font = new Font("Microsoft YaHei UI", 9F);
            txtSummary.Location = new Point(6, 4);
            txtSummary.Multiline = true;
            txtSummary.Name = "txtSummary";
            txtSummary.ReadOnly = true;
            txtSummary.ScrollBars = ScrollBars.Vertical;
            txtSummary.Size = new Size(494, 64);
            txtSummary.TabIndex = 1;
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.Black;
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.ForeColor = Color.LightGreen;
            txtLog.Location = new Point(11, 536);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1012, 134);
            txtLog.TabIndex = 2;
            // 
            // BaiduPanAnalysisForm
            // 
            ClientSize = new Size(1034, 681);
            Controls.Add(mainPanel);
            MinimumSize = new Size(900, 600);
            Name = "BaiduPanAnalysisForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "百度网盘 AI 分析";
            Load += BaiduPanAnalysisForm_Load;
            mainPanel.ResumeLayout(false);
            mainPanel.PerformLayout();
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            contentPanel.Panel1.ResumeLayout(false);
            contentPanel.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)contentPanel).EndInit();
            contentPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            rightPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        #region 控件字段声明

        private System.Windows.Forms.TableLayoutPanel mainPanel;
        private System.Windows.Forms.FlowLayoutPanel topPanel;
        private System.Windows.Forms.SplitContainer contentPanel;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Label lblDepth;
        private System.Windows.Forms.ComboBox cmbDepth;
        private System.Windows.Forms.CheckBox chkDetectDuplicates;
        private System.Windows.Forms.CheckBox chkUseCache;
        private System.Windows.Forms.Button btnStartAnalysis;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.TextBox txtSummary; 
        private System.Windows.Forms.ListView lstRecommendations;
        private System.Windows.Forms.TextBox txtLog;

        #endregion
    }
}
