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
            btnOpenOrganizer = new Button();
            btnGoUp = new Button();
            contentPanel = new SplitContainer();
            leftPanel = new Panel();
            txtSummary = new TextBox();
            treeFolders = new TreeView();
            rightPanel = new Panel();
            tabControl = new TabControl();
            tabRecommendations = new TabPage();
            lstRecommendations = new ListView();
            tabTags = new TabPage();
            pnlTagFilter = new FlowLayoutPanel();
            lblTagSubject = new Label();
            cmbTagSubject = new ComboBox();
            lblTagValues = new Label();
            cmbTagValues = new ComboBox();
            lblTagAge = new Label();
            cmbTagAge = new ComboBox();
            btnTagFilter = new Button();
            btnTagReset = new Button();
            lstFileTags = new ListView();
            pnlTagActions = new FlowLayoutPanel();
            btnDeleteTagged = new Button();
            btnMoveTagged = new Button();
            txtLog = new TextBox();
            splitContainer1 = new SplitContainer();
            pnlSummaryActions = new FlowLayoutPanel();
            btnCopySummary = new Button();
            btnSaveSummary = new Button();
            btnUploadSummary = new Button();
            tabAiPayload = new TabPage();
            pnlAiPayloadActions = new FlowLayoutPanel();
            btnCopyAiPayload = new Button();
            btnOpenAiPanel = new Button();
            txtAiPayload = new TextBox();
            tabParseResult = new TabPage();
            pnlParseActions = new FlowLayoutPanel();
            lblParseHint = new Label();
            btnParseAiResult = new Button();
            btnClearParseInput = new Button();
            btnSampleJson = new Button();
            txtAiResultInput = new TextBox();
            tabParseResult.SuspendLayout();
            pnlParseActions.SuspendLayout();
            mainPanel.SuspendLayout();
            topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)contentPanel).BeginInit();
            contentPanel.Panel1.SuspendLayout();
            contentPanel.Panel2.SuspendLayout();
            contentPanel.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            tabControl.SuspendLayout();
            tabRecommendations.SuspendLayout();
            tabTags.SuspendLayout();
            pnlTagFilter.SuspendLayout();
            pnlTagActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
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
            topPanel.Controls.Add(btnOpenOrganizer);
            topPanel.Controls.Add(btnGoUp);
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
            // btnOpenOrganizer
            // 
            btnOpenOrganizer.Location = new Point(972, 10);
            btnOpenOrganizer.Margin = new Padding(4);
            btnOpenOrganizer.Name = "btnOpenOrganizer";
            btnOpenOrganizer.Size = new Size(130, 23);
            btnOpenOrganizer.TabIndex = 9;
            btnOpenOrganizer.Text = "🧰 打开整理工具";
            btnOpenOrganizer.Click += btnOpenOrganizer_Click;
            // 
            // btnGoUp
            // 
            btnGoUp.Enabled = false;
            btnGoUp.Location = new Point(8, 47);
            btnGoUp.Margin = new Padding(4);
            btnGoUp.Name = "btnGoUp";
            btnGoUp.Size = new Size(90, 23);
            btnGoUp.TabIndex = 9;
            btnGoUp.Text = "⬆️ 返回上级";
            btnGoUp.Click += btnGoUp_Click;
            // 
            // contentPanel
            // 
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(11, 55);
            contentPanel.Name = "contentPanel";
            // 
            // contentPanel.Panel1
            // 
            contentPanel.Panel1.Controls.Add(leftPanel);
            contentPanel.Panel1MinSize = 260;
            // 
            // contentPanel.Panel2
            // 
            contentPanel.Panel2.Controls.Add(rightPanel);
            contentPanel.Panel2MinSize = 500;
            contentPanel.Size = new Size(1012, 475);
            contentPanel.SplitterDistance = 508;
            contentPanel.TabIndex = 1;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(splitContainer1);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Padding = new Padding(0, 4, 6, 0);
            leftPanel.Size = new Size(508, 475);
            leftPanel.TabIndex = 0;
            // 
            // txtSummary
            // 
            txtSummary.BackColor = SystemColors.Info;
            txtSummary.Dock = DockStyle.Fill;
            txtSummary.Font = new Font("Microsoft YaHei UI", 9F);
            txtSummary.Location = new Point(0, 0);
            txtSummary.Multiline = true;
            txtSummary.Name = "txtSummary";
            txtSummary.ReadOnly = false;
            txtSummary.ScrollBars = ScrollBars.Vertical;
            txtSummary.Size = new Size(502, 135);
            txtSummary.TabIndex = 1;
            // 
            // pnlSummaryActions
            // 
            pnlSummaryActions.Controls.Add(btnUploadSummary);
            pnlSummaryActions.Controls.Add(btnSaveSummary);
            pnlSummaryActions.Controls.Add(btnCopySummary);
            pnlSummaryActions.Dock = DockStyle.Top;
            pnlSummaryActions.FlowDirection = FlowDirection.RightToLeft;
            pnlSummaryActions.Location = new Point(0, 0);
            pnlSummaryActions.Name = "pnlSummaryActions";
            pnlSummaryActions.Padding = new Padding(2, 2, 2, 2);
            pnlSummaryActions.Size = new Size(502, 34);
            pnlSummaryActions.TabIndex = 2;
            // 
            // btnCopySummary
            // 
            btnCopySummary.Location = new Point(424, 2);
            btnCopySummary.Margin = new Padding(3, 3, 3, 3);
            btnCopySummary.Name = "btnCopySummary";
            btnCopySummary.Size = new Size(76, 28);
            btnCopySummary.TabIndex = 2;
            btnCopySummary.Text = "📋 复制";
            btnCopySummary.Click += btnCopySummary_Click;
            // 
            // btnSaveSummary
            // 
            btnSaveSummary.Location = new Point(341, 2);
            btnSaveSummary.Margin = new Padding(3, 3, 3, 3);
            btnSaveSummary.Name = "btnSaveSummary";
            btnSaveSummary.Size = new Size(77, 28);
            btnSaveSummary.TabIndex = 1;
            btnSaveSummary.Text = "💾 本地保存";
            btnSaveSummary.Click += btnSaveSummary_Click;
            // 
            // btnUploadSummary
            // 
            btnUploadSummary.Location = new Point(243, 2);
            btnUploadSummary.Margin = new Padding(3, 3, 3, 3);
            btnUploadSummary.Name = "btnUploadSummary";
            btnUploadSummary.Size = new Size(92, 28);
            btnUploadSummary.TabIndex = 0;
            btnUploadSummary.Text = "📤 上传网盘";
            btnUploadSummary.Click += btnUploadSummary_Click;
            // 
            // treeFolders
            // 
            treeFolders.Dock = DockStyle.Fill;
            treeFolders.Font = new Font("Microsoft YaHei UI", 9F);
            treeFolders.HideSelection = false;
            treeFolders.Location = new Point(0, 0);
            treeFolders.Name = "treeFolders";
            treeFolders.Size = new Size(502, 332);
            treeFolders.TabIndex = 2;
            treeFolders.NodeMouseDoubleClick += treeFolders_NodeMouseDoubleClick;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(tabControl);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(0, 0);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(6, 4, 0, 0);
            rightPanel.Size = new Size(500, 475);
            rightPanel.TabIndex = 0;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabRecommendations);
            tabControl.Controls.Add(tabTags);
            tabControl.Controls.Add(tabAiPayload);
            tabControl.Controls.Add(tabParseResult);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Microsoft YaHei UI", 9F);
            tabControl.Location = new Point(6, 4);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(494, 471);
            tabControl.TabIndex = 0;
            // 
            // tabRecommendations
            // 
            tabRecommendations.Controls.Add(lstRecommendations);
            tabRecommendations.Location = new Point(4, 26);
            tabRecommendations.Name = "tabRecommendations";
            tabRecommendations.Padding = new Padding(3);
            tabRecommendations.Size = new Size(486, 441);
            tabRecommendations.TabIndex = 0;
            tabRecommendations.Text = "📋 整理建议";
            tabRecommendations.UseVisualStyleBackColor = true;
            // 
            // lstRecommendations
            // 
            lstRecommendations.CheckBoxes = true;
            lstRecommendations.Dock = DockStyle.Fill;
            lstRecommendations.FullRowSelect = true;
            lstRecommendations.GridLines = true;
            lstRecommendations.Location = new Point(3, 3);
            lstRecommendations.Name = "lstRecommendations";
            lstRecommendations.Size = new Size(480, 435);
            lstRecommendations.TabIndex = 0;
            lstRecommendations.UseCompatibleStateImageBehavior = false;
            lstRecommendations.View = View.Details;
            // 
            // tabTags
            // 
            tabTags.Controls.Add(pnlTagActions);
            tabTags.Controls.Add(lstFileTags);
            tabTags.Controls.Add(pnlTagFilter);
            tabTags.Location = new Point(4, 26);
            tabTags.Name = "tabTags";
            tabTags.Padding = new Padding(3);
            tabTags.Size = new Size(486, 441);
            tabTags.TabIndex = 1;
            tabTags.Text = "🏷️ 文件打标";
            tabTags.UseVisualStyleBackColor = true;
            // 
            // pnlTagFilter
            // 
            pnlTagFilter.Controls.Add(lblTagSubject);
            pnlTagFilter.Controls.Add(cmbTagSubject);
            pnlTagFilter.Controls.Add(lblTagValues);
            pnlTagFilter.Controls.Add(cmbTagValues);
            pnlTagFilter.Controls.Add(lblTagAge);
            pnlTagFilter.Controls.Add(cmbTagAge);
            pnlTagFilter.Controls.Add(btnTagFilter);
            pnlTagFilter.Controls.Add(btnTagReset);
            pnlTagFilter.Dock = DockStyle.Top;
            pnlTagFilter.Location = new Point(3, 3);
            pnlTagFilter.Name = "pnlTagFilter";
            pnlTagFilter.Padding = new Padding(2, 4, 2, 2);
            pnlTagFilter.Size = new Size(480, 38);
            pnlTagFilter.TabIndex = 0;
            // 
            // lblTagSubject
            // 
            lblTagSubject.AutoSize = true;
            lblTagSubject.Location = new Point(5, 11);
            lblTagSubject.Margin = new Padding(3, 7, 2, 3);
            lblTagSubject.Name = "lblTagSubject";
            lblTagSubject.Size = new Size(44, 17);
            lblTagSubject.TabIndex = 0;
            lblTagSubject.Text = "科目：";
            // 
            // cmbTagSubject
            // 
            cmbTagSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTagSubject.Location = new Point(54, 7);
            cmbTagSubject.Margin = new Padding(3, 3, 10, 3);
            cmbTagSubject.Name = "cmbTagSubject";
            cmbTagSubject.Size = new Size(96, 25);
            cmbTagSubject.TabIndex = 1;
            // 
            // lblTagValues
            // 
            lblTagValues.AutoSize = true;
            lblTagValues.Location = new Point(163, 11);
            lblTagValues.Margin = new Padding(3, 7, 2, 3);
            lblTagValues.Name = "lblTagValues";
            lblTagValues.Size = new Size(75, 17);
            lblTagValues.TabIndex = 2;
            lblTagValues.Text = "价值观：";
            // 
            // cmbTagValues
            // 
            cmbTagValues.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTagValues.Location = new Point(243, 7);
            cmbTagValues.Margin = new Padding(3, 3, 10, 3);
            cmbTagValues.Name = "cmbTagValues";
            cmbTagValues.Size = new Size(70, 25);
            cmbTagValues.TabIndex = 3;
            // 
            // lblTagAge
            // 
            lblTagAge.AutoSize = true;
            lblTagAge.Location = new Point(326, 11);
            lblTagAge.Margin = new Padding(3, 7, 2, 3);
            lblTagAge.Name = "lblTagAge";
            lblTagAge.Size = new Size(60, 17);
            lblTagAge.TabIndex = 4;
            lblTagAge.Text = "年龄段：";
            // 
            // cmbTagAge
            // 
            cmbTagAge.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTagAge.Location = new Point(391, 7);
            cmbTagAge.Margin = new Padding(3, 3, 10, 3);
            cmbTagAge.Name = "cmbTagAge";
            cmbTagAge.Size = new Size(80, 25);
            cmbTagAge.TabIndex = 5;
            // 
            // btnTagFilter
            // 
            btnTagFilter.Location = new Point(484, 7);
            btnTagFilter.Margin = new Padding(3, 3, 3, 3);
            btnTagFilter.Name = "btnTagFilter";
            btnTagFilter.Size = new Size(72, 24);
            btnTagFilter.TabIndex = 6;
            btnTagFilter.Text = "筛选";
            btnTagFilter.Click += btnTagFilter_Click;
            // 
            // btnTagReset
            // 
            btnTagReset.Location = new Point(562, 7);
            btnTagReset.Margin = new Padding(3, 3, 3, 3);
            btnTagReset.Name = "btnTagReset";
            btnTagReset.Size = new Size(60, 24);
            btnTagReset.TabIndex = 7;
            btnTagReset.Text = "重置";
            btnTagReset.Click += btnTagReset_Click;
            // 
            // lstFileTags
            // 
            lstFileTags.CheckBoxes = true;
            lstFileTags.Dock = DockStyle.Fill;
            lstFileTags.FullRowSelect = true;
            lstFileTags.GridLines = true;
            lstFileTags.Location = new Point(3, 41);
            lstFileTags.Name = "lstFileTags";
            lstFileTags.Size = new Size(480, 355);
            lstFileTags.TabIndex = 1;
            lstFileTags.UseCompatibleStateImageBehavior = false;
            lstFileTags.View = View.Details;
            // 
            // pnlTagActions
            // 
            pnlTagActions.Controls.Add(btnDeleteTagged);
            pnlTagActions.Controls.Add(btnMoveTagged);
            pnlTagActions.Dock = DockStyle.Bottom;
            pnlTagActions.FlowDirection = FlowDirection.RightToLeft;
            pnlTagActions.Location = new Point(3, 396);
            pnlTagActions.Name = "pnlTagActions";
            pnlTagActions.Padding = new Padding(2);
            pnlTagActions.Size = new Size(480, 42);
            pnlTagActions.TabIndex = 2;
            // 
            // btnDeleteTagged
            // 
            btnDeleteTagged.Location = new Point(396, 5);
            btnDeleteTagged.Margin = new Padding(3, 3, 3, 3);
            btnDeleteTagged.Name = "btnDeleteTagged";
            btnDeleteTagged.Size = new Size(78, 30);
            btnDeleteTagged.TabIndex = 0;
            btnDeleteTagged.Text = "🗑️ 删除选中";
            btnDeleteTagged.Click += btnDeleteTagged_Click;
            // 
            // btnMoveTagged
            // 
            btnMoveTagged.Location = new Point(312, 5);
            btnMoveTagged.Margin = new Padding(3, 3, 3, 3);
            btnMoveTagged.Name = "btnMoveTagged";
            btnMoveTagged.Size = new Size(78, 30);
            btnMoveTagged.TabIndex = 1;
            btnMoveTagged.Text = "📦 移动选中";
            btnMoveTagged.Click += btnMoveTagged_Click;
            // 
            // tabAiPayload
            // 
            tabAiPayload.Controls.Add(pnlAiPayloadActions);
            tabAiPayload.Controls.Add(txtAiPayload);
            tabAiPayload.Location = new Point(4, 26);
            tabAiPayload.Name = "tabAiPayload";
            tabAiPayload.Padding = new Padding(3);
            tabAiPayload.Size = new Size(486, 441);
            tabAiPayload.TabIndex = 2;
            tabAiPayload.Text = "📤 AI 发送内容";
            tabAiPayload.UseVisualStyleBackColor = true;
            // 
            // pnlAiPayloadActions
            // 
            pnlAiPayloadActions.Controls.Add(btnCopyAiPayload);
            pnlAiPayloadActions.Controls.Add(btnOpenAiPanel);
            pnlAiPayloadActions.Dock = DockStyle.Top;
            pnlAiPayloadActions.FlowDirection = FlowDirection.RightToLeft;
            pnlAiPayloadActions.Location = new Point(3, 3);
            pnlAiPayloadActions.Name = "pnlAiPayloadActions";
            pnlAiPayloadActions.Padding = new Padding(2, 2, 2, 2);
            pnlAiPayloadActions.Size = new Size(480, 36);
            pnlAiPayloadActions.TabIndex = 0;
            // 
            // btnCopyAiPayload
            // 
            btnCopyAiPayload.Location = new Point(394, 2);
            btnCopyAiPayload.Margin = new Padding(3, 3, 3, 3);
            btnCopyAiPayload.Name = "btnCopyAiPayload";
            btnCopyAiPayload.Size = new Size(84, 28);
            btnCopyAiPayload.TabIndex = 1;
            btnCopyAiPayload.Text = "📋 复制全部";
            btnCopyAiPayload.Click += btnCopyAiPayload_Click;
            // 
            // btnOpenAiPanel
            // 
            btnOpenAiPanel.Location = new Point(268, 2);
            btnOpenAiPanel.Margin = new Padding(3, 3, 3, 3);
            btnOpenAiPanel.Name = "btnOpenAiPanel";
            btnOpenAiPanel.Size = new Size(120, 28);
            btnOpenAiPanel.TabIndex = 0;
            btnOpenAiPanel.Text = "🤖 打开AI面板";
            btnOpenAiPanel.Click += btnOpenAiPanel_Click;
            // 
            // txtAiPayload
            // 
            txtAiPayload.Dock = DockStyle.Fill;
            txtAiPayload.Font = new Font("Consolas", 9F);
            txtAiPayload.Location = new Point(3, 39);
            txtAiPayload.Multiline = true;
            txtAiPayload.Name = "txtAiPayload";
            txtAiPayload.ReadOnly = true;
            txtAiPayload.ScrollBars = ScrollBars.Both;
            txtAiPayload.Size = new Size(480, 399);
            txtAiPayload.TabIndex = 1;
            txtAiPayload.WordWrap = false;
            // 
            // tabParseResult
            // 
            tabParseResult.Controls.Add(txtAiResultInput);
            tabParseResult.Controls.Add(lblParseHint);
            tabParseResult.Controls.Add(pnlParseActions);
            tabParseResult.Location = new Point(4, 26);
            tabParseResult.Name = "tabParseResult";
            tabParseResult.Padding = new Padding(3);
            tabParseResult.Size = new Size(486, 441);
            tabParseResult.TabIndex = 3;
            tabParseResult.Text = "📥 解析AI结果";
            tabParseResult.UseVisualStyleBackColor = true;
            // 
            // pnlParseActions
            // 
            pnlParseActions.Controls.Add(btnParseAiResult);
            pnlParseActions.Controls.Add(btnClearParseInput);
            pnlParseActions.Controls.Add(btnSampleJson);
            pnlParseActions.Dock = DockStyle.Top;
            pnlParseActions.FlowDirection = FlowDirection.RightToLeft;
            pnlParseActions.Location = new Point(3, 3);
            pnlParseActions.Name = "pnlParseActions";
            pnlParseActions.Padding = new Padding(2, 2, 2, 2);
            pnlParseActions.Size = new Size(480, 36);
            pnlParseActions.TabIndex = 0;
            // 
            // btnParseAiResult
            // 
            btnParseAiResult.Location = new Point(381, 2);
            btnParseAiResult.Margin = new Padding(3, 3, 3, 3);
            btnParseAiResult.Name = "btnParseAiResult";
            btnParseAiResult.Size = new Size(94, 28);
            btnParseAiResult.TabIndex = 0;
            btnParseAiResult.Text = "🔍 解析并填充";
            btnParseAiResult.Click += btnParseAiResult_Click;
            // 
            // btnClearParseInput
            // 
            btnClearParseInput.Location = new Point(300, 2);
            btnClearParseInput.Margin = new Padding(3, 3, 3, 3);
            btnClearParseInput.Name = "btnClearParseInput";
            btnClearParseInput.Size = new Size(78, 28);
            btnClearParseInput.TabIndex = 1;
            btnClearParseInput.Text = "🧹 清空";
            btnClearParseInput.Click += btnClearParseInput_Click;
            // 
            // btnSampleJson
            // 
            btnSampleJson.Location = new Point(200, 2);
            btnSampleJson.Margin = new Padding(3, 3, 3, 3);
            btnSampleJson.Name = "btnSampleJson";
            btnSampleJson.Size = new Size(78, 28);
            btnSampleJson.TabIndex = 2;
            btnSampleJson.Text = "📄 示例";
            btnSampleJson.Click += btnSampleJson_Click;
            // 
            // lblParseHint
            // 
            lblParseHint.Dock = DockStyle.Top;
            lblParseHint.Location = new Point(3, 39);
            lblParseHint.Name = "lblParseHint";
            lblParseHint.Padding = new Padding(4, 3, 4, 3);
            lblParseHint.Size = new Size(480, 40);
            lblParseHint.TabIndex = 1;
            lblParseHint.Text = "将 AI 返回的 JSON 结果粘贴到下方（可包含 Markdown 代码块），支持中文 type 与常见字段别名。\r\n点击「解析并填充」后，整理建议与文件打标将自动生成，无需 AI API；可点「示例」查看格式。";
            // 
            // txtAiResultInput
            // 
            txtAiResultInput.Dock = DockStyle.Fill;
            txtAiResultInput.Font = new Font("Consolas", 9F);
            txtAiResultInput.Location = new Point(3, 79);
            txtAiResultInput.Multiline = true;
            txtAiResultInput.Name = "txtAiResultInput";
            txtAiResultInput.ScrollBars = ScrollBars.Both;
            txtAiResultInput.Size = new Size(480, 359);
            txtAiResultInput.TabIndex = 2;
            txtAiResultInput.WordWrap = false;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(11, 536);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1012, 134);
            txtLog.TabIndex = 2;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(treeFolders);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(txtSummary);
            splitContainer1.Panel2.Controls.Add(pnlSummaryActions);
            splitContainer1.Size = new Size(502, 471);
            splitContainer1.SplitterDistance = 332;
            splitContainer1.TabIndex = 3;
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
            leftPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabRecommendations.ResumeLayout(false);
            tabTags.ResumeLayout(false);
            tabParseResult.ResumeLayout(false);
            pnlParseActions.ResumeLayout(false);
            pnlParseActions.PerformLayout();
            pnlTagFilter.ResumeLayout(false);
            pnlTagFilter.PerformLayout();
            pnlTagActions.ResumeLayout(false);
            pnlTagActions.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnOpenOrganizer;
        private System.Windows.Forms.Button btnGoUp;
        private System.Windows.Forms.TreeView treeFolders;
        private System.Windows.Forms.TextBox txtSummary; 
        private System.Windows.Forms.ListView lstRecommendations;
        private System.Windows.Forms.TextBox txtLog;

        #endregion

        private SplitContainer splitContainer1;

        // === 文件打标（TabControl + 筛选 + 批量整理）===
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabRecommendations;
        private System.Windows.Forms.TabPage tabTags;
        private System.Windows.Forms.FlowLayoutPanel pnlTagFilter;
        private System.Windows.Forms.Label lblTagSubject;
        private System.Windows.Forms.ComboBox cmbTagSubject;
        private System.Windows.Forms.Label lblTagValues;
        private System.Windows.Forms.ComboBox cmbTagValues;
        private System.Windows.Forms.Label lblTagAge;
        private System.Windows.Forms.ComboBox cmbTagAge;
        private System.Windows.Forms.Button btnTagFilter;
        private System.Windows.Forms.Button btnTagReset;
        private System.Windows.Forms.ListView lstFileTags;
        private System.Windows.Forms.FlowLayoutPanel pnlTagActions;
        private System.Windows.Forms.Button btnDeleteTagged;
        private System.Windows.Forms.Button btnMoveTagged;

        // === 摘要持久化/上传 + AI 发送内容标签页 ===
        private System.Windows.Forms.FlowLayoutPanel pnlSummaryActions;
        private System.Windows.Forms.Button btnCopySummary;
        private System.Windows.Forms.Button btnSaveSummary;
        private System.Windows.Forms.Button btnUploadSummary;
        private System.Windows.Forms.TabPage tabAiPayload;
        private System.Windows.Forms.FlowLayoutPanel pnlAiPayloadActions;
        private System.Windows.Forms.Button btnCopyAiPayload;
        private System.Windows.Forms.Button btnOpenAiPanel;
        private System.Windows.Forms.TextBox txtAiPayload;

        // === 解析 AI 结果标签页（手动粘贴解析，无需 AI API）===
        private System.Windows.Forms.TabPage tabParseResult;
        private System.Windows.Forms.FlowLayoutPanel pnlParseActions;
        private System.Windows.Forms.Label lblParseHint;
        private System.Windows.Forms.Button btnParseAiResult;
        private System.Windows.Forms.Button btnClearParseInput;
        private System.Windows.Forms.Button btnSampleJson;
        private System.Windows.Forms.TextBox txtAiResultInput;
    }
}
