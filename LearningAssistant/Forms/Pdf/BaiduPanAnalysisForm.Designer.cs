using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Forms.Pdf
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
            btnPullSnapshot = new Button();
            btnAIAnalysis = new Button();
            btnClearSnapshot = new Button();
            btnCancel = new Button();
            btnExecute = new Button();
            btnOpenOrganizer = new Button();
            btnGoUp = new Button();
            contentPanel = new SplitContainer();
            leftPanel = new Panel();
            splitContainer1 = new SplitContainer();
            treeFolders = new TreeView();
            txtSummary = new TextBox();
            pnlSummaryActions = new FlowLayoutPanel();
            btnUploadSummary = new Button();
            btnSaveSummary = new Button();
            btnCopySummary = new Button();
            rightPanel = new Panel();
            tabControl = new TabControl();
            tabRecommendations = new TabPage();
            lstRecommendations = new ListView();
            tabTags = new TabPage();
            pnlTagActions = new FlowLayoutPanel();
            btnDeleteTagged = new Button();
            btnMoveTagged = new Button();
            lstFileTags = new ListView();
            pnlTagFilter = new FlowLayoutPanel();
            lblTagSubject = new Label();
            cmbTagSubject = new ComboBox();
            lblTagValues = new Label();
            cmbTagValues = new ComboBox();
            lblTagAge = new Label();
            cmbTagAge = new ComboBox();
            btnTagFilter = new Button();
            btnTagReset = new Button();
            tabAiPayload = new TabPage();
            pnlAiPayloadActions = new FlowLayoutPanel();
            btnCopyAiPayload = new Button();
            btnOpenAiPanel = new Button();
            txtAiPayload = new TextBox();
            tabParseResult = new TabPage();
            txtAiResultInput = new TextBox();
            lblParseHint = new Label();
            pnlParseActions = new FlowLayoutPanel();
            btnParseAiResult = new Button();
            btnClearParseInput = new Button();
            btnSampleJson = new Button();
            txtLog = new TextBox();
            mainPanel.SuspendLayout();
            topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)contentPanel).BeginInit();
            contentPanel.Panel1.SuspendLayout();
            contentPanel.Panel2.SuspendLayout();
            contentPanel.SuspendLayout();
            leftPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            pnlSummaryActions.SuspendLayout();
            rightPanel.SuspendLayout();
            tabControl.SuspendLayout();
            tabRecommendations.SuspendLayout();
            tabTags.SuspendLayout();
            pnlTagActions.SuspendLayout();
            pnlTagFilter.SuspendLayout();
            tabAiPayload.SuspendLayout();
            pnlAiPayloadActions.SuspendLayout();
            tabParseResult.SuspendLayout();
            pnlParseActions.SuspendLayout();
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
            mainPanel.Size = new Size(1488, 627);
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
            topPanel.Controls.Add(btnPullSnapshot);
            topPanel.Controls.Add(btnAIAnalysis);
            topPanel.Controls.Add(btnClearSnapshot);
            topPanel.Controls.Add(btnCancel);
            topPanel.Controls.Add(btnExecute);
            topPanel.Controls.Add(btnOpenOrganizer);
            topPanel.Controls.Add(btnGoUp);
            topPanel.Dock = DockStyle.Fill;
            topPanel.Location = new Point(11, 11);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(4, 6, 4, 6);
            topPanel.Size = new Size(1466, 38);
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
            // btnPullSnapshot
            // 
            btnPullSnapshot.Location = new Point(632, 10);
            btnPullSnapshot.Margin = new Padding(8, 4, 4, 4);
            btnPullSnapshot.Name = "btnPullSnapshot";
            btnPullSnapshot.Size = new Size(100, 23);
            btnPullSnapshot.TabIndex = 6;
            btnPullSnapshot.Text = "📥 拉取快照";
            btnPullSnapshot.Click += btnPullSnapshot_Click;
            // 
            // btnAIAnalysis
            // 
            btnAIAnalysis.Enabled = false;
            btnAIAnalysis.Location = new Point(740, 10);
            btnAIAnalysis.Margin = new Padding(4);
            btnAIAnalysis.Name = "btnAIAnalysis";
            btnAIAnalysis.Size = new Size(90, 23);
            btnAIAnalysis.TabIndex = 7;
            btnAIAnalysis.Text = "🤖 AI分析";
            btnAIAnalysis.Click += btnAIAnalysis_Click;
            // 
            // btnClearSnapshot
            // 
            btnClearSnapshot.Enabled = false;
            btnClearSnapshot.Location = new Point(838, 10);
            btnClearSnapshot.Margin = new Padding(4);
            btnClearSnapshot.Name = "btnClearSnapshot";
            btnClearSnapshot.Size = new Size(90, 23);
            btnClearSnapshot.TabIndex = 8;
            btnClearSnapshot.Text = "🗑 清除快照";
            btnClearSnapshot.Click += btnClearSnapshot_Click;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Location = new Point(936, 10);
            btnCancel.Margin = new Padding(4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(70, 23);
            btnCancel.TabIndex = 9;
            btnCancel.Text = "取消";
            btnCancel.Click += btnCancel_Click;
            // 
            // btnExecute
            // 
            btnExecute.Location = new Point(1014, 10);
            btnExecute.Margin = new Padding(4);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new Size(140, 23);
            btnExecute.TabIndex = 10;
            btnExecute.Text = "✅ 执行选中操作";
            btnExecute.Click += btnExecute_Click;
            // 
            // btnOpenOrganizer
            // 
            btnOpenOrganizer.Location = new Point(1162, 10);
            btnOpenOrganizer.Margin = new Padding(4);
            btnOpenOrganizer.Name = "btnOpenOrganizer";
            btnOpenOrganizer.Size = new Size(93, 23);
            btnOpenOrganizer.TabIndex = 11;
            btnOpenOrganizer.Text = "\U0001f9f0 手工整理";
            btnOpenOrganizer.Click += btnOpenOrganizer_Click;
            // 
            // btnGoUp
            // 
            btnGoUp.Enabled = false;
            btnGoUp.Location = new Point(1263, 10);
            btnGoUp.Margin = new Padding(4);
            btnGoUp.Name = "btnGoUp";
            btnGoUp.Size = new Size(90, 23);
            btnGoUp.TabIndex = 12;
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
            contentPanel.Size = new Size(1466, 421);
            contentPanel.SplitterDistance = 735;
            contentPanel.TabIndex = 1;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(splitContainer1);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Padding = new Padding(0, 4, 6, 0);
            leftPanel.Size = new Size(735, 421);
            leftPanel.TabIndex = 0;
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
            splitContainer1.Size = new Size(729, 417);
            splitContainer1.SplitterDistance = 293;
            splitContainer1.TabIndex = 3;
            // 
            // treeFolders
            // 
            treeFolders.Dock = DockStyle.Fill;
            treeFolders.Font = new Font("Microsoft YaHei UI", 9F);
            treeFolders.HideSelection = false;
            treeFolders.Location = new Point(0, 0);
            treeFolders.Name = "treeFolders";
            treeFolders.Size = new Size(729, 293);
            treeFolders.TabIndex = 2;
            treeFolders.NodeMouseDoubleClick += treeFolders_NodeMouseDoubleClick;
            // 
            // txtSummary
            // 
            txtSummary.BackColor = SystemColors.Info;
            txtSummary.Dock = DockStyle.Fill;
            txtSummary.Font = new Font("Microsoft YaHei UI", 9F);
            txtSummary.Location = new Point(0, 34);
            txtSummary.Multiline = true;
            txtSummary.Name = "txtSummary";
            txtSummary.ScrollBars = ScrollBars.Vertical;
            txtSummary.Size = new Size(729, 86);
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
            pnlSummaryActions.Padding = new Padding(2);
            pnlSummaryActions.Size = new Size(729, 34);
            pnlSummaryActions.TabIndex = 2;
            // 
            // btnUploadSummary
            // 
            btnUploadSummary.Location = new Point(630, 5);
            btnUploadSummary.Name = "btnUploadSummary";
            btnUploadSummary.Size = new Size(92, 28);
            btnUploadSummary.TabIndex = 0;
            btnUploadSummary.Text = "📤 上传网盘";
            btnUploadSummary.Click += btnUploadSummary_Click;
            // 
            // btnSaveSummary
            // 
            btnSaveSummary.Location = new Point(547, 5);
            btnSaveSummary.Name = "btnSaveSummary";
            btnSaveSummary.Size = new Size(77, 28);
            btnSaveSummary.TabIndex = 1;
            btnSaveSummary.Text = "💾 本地保存";
            btnSaveSummary.Click += btnSaveSummary_Click;
            // 
            // btnCopySummary
            // 
            btnCopySummary.Location = new Point(465, 5);
            btnCopySummary.Name = "btnCopySummary";
            btnCopySummary.Size = new Size(76, 28);
            btnCopySummary.TabIndex = 2;
            btnCopySummary.Text = "📋 复制";
            btnCopySummary.Click += btnCopySummary_Click;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(tabControl);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(0, 0);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(6, 4, 0, 0);
            rightPanel.Size = new Size(727, 421);
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
            tabControl.Size = new Size(721, 417);
            tabControl.TabIndex = 0;
            // 
            // tabRecommendations
            // 
            tabRecommendations.Controls.Add(lstRecommendations);
            tabRecommendations.Location = new Point(4, 26);
            tabRecommendations.Name = "tabRecommendations";
            tabRecommendations.Padding = new Padding(3);
            tabRecommendations.Size = new Size(713, 387);
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
            lstRecommendations.Size = new Size(707, 381);
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
            tabTags.Size = new Size(713, 387);
            tabTags.TabIndex = 1;
            tabTags.Text = "🏷️ 文件打标";
            tabTags.UseVisualStyleBackColor = true;
            // 
            // pnlTagActions
            // 
            pnlTagActions.Controls.Add(btnDeleteTagged);
            pnlTagActions.Controls.Add(btnMoveTagged);
            pnlTagActions.Dock = DockStyle.Bottom;
            pnlTagActions.FlowDirection = FlowDirection.RightToLeft;
            pnlTagActions.Location = new Point(3, 342);
            pnlTagActions.Name = "pnlTagActions";
            pnlTagActions.Padding = new Padding(2);
            pnlTagActions.Size = new Size(707, 42);
            pnlTagActions.TabIndex = 2;
            // 
            // btnDeleteTagged
            // 
            btnDeleteTagged.Location = new Point(622, 5);
            btnDeleteTagged.Name = "btnDeleteTagged";
            btnDeleteTagged.Size = new Size(78, 30);
            btnDeleteTagged.TabIndex = 0;
            btnDeleteTagged.Text = "🗑️ 删除选中";
            btnDeleteTagged.Click += btnDeleteTagged_Click;
            // 
            // btnMoveTagged
            // 
            btnMoveTagged.Location = new Point(538, 5);
            btnMoveTagged.Name = "btnMoveTagged";
            btnMoveTagged.Size = new Size(78, 30);
            btnMoveTagged.TabIndex = 1;
            btnMoveTagged.Text = "📦 移动选中";
            btnMoveTagged.Click += btnMoveTagged_Click;
            // 
            // lstFileTags
            // 
            lstFileTags.CheckBoxes = true;
            lstFileTags.Dock = DockStyle.Fill;
            lstFileTags.FullRowSelect = true;
            lstFileTags.GridLines = true;
            lstFileTags.Location = new Point(3, 41);
            lstFileTags.Name = "lstFileTags";
            lstFileTags.Size = new Size(707, 343);
            lstFileTags.TabIndex = 1;
            lstFileTags.UseCompatibleStateImageBehavior = false;
            lstFileTags.View = View.Details;
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
            pnlTagFilter.Size = new Size(707, 38);
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
            lblTagValues.Size = new Size(56, 17);
            lblTagValues.TabIndex = 2;
            lblTagValues.Text = "价值观：";
            // 
            // cmbTagValues
            // 
            cmbTagValues.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTagValues.Location = new Point(224, 7);
            cmbTagValues.Margin = new Padding(3, 3, 10, 3);
            cmbTagValues.Name = "cmbTagValues";
            cmbTagValues.Size = new Size(70, 25);
            cmbTagValues.TabIndex = 3;
            // 
            // lblTagAge
            // 
            lblTagAge.AutoSize = true;
            lblTagAge.Location = new Point(307, 11);
            lblTagAge.Margin = new Padding(3, 7, 2, 3);
            lblTagAge.Name = "lblTagAge";
            lblTagAge.Size = new Size(56, 17);
            lblTagAge.TabIndex = 4;
            lblTagAge.Text = "年龄段：";
            // 
            // cmbTagAge
            // 
            cmbTagAge.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTagAge.Location = new Point(368, 7);
            cmbTagAge.Margin = new Padding(3, 3, 10, 3);
            cmbTagAge.Name = "cmbTagAge";
            cmbTagAge.Size = new Size(80, 25);
            cmbTagAge.TabIndex = 5;
            // 
            // btnTagFilter
            // 
            btnTagFilter.Location = new Point(461, 7);
            btnTagFilter.Name = "btnTagFilter";
            btnTagFilter.Size = new Size(72, 24);
            btnTagFilter.TabIndex = 6;
            btnTagFilter.Text = "筛选";
            btnTagFilter.Click += btnTagFilter_Click;
            // 
            // btnTagReset
            // 
            btnTagReset.Location = new Point(539, 7);
            btnTagReset.Name = "btnTagReset";
            btnTagReset.Size = new Size(60, 24);
            btnTagReset.TabIndex = 7;
            btnTagReset.Text = "重置";
            btnTagReset.Click += btnTagReset_Click;
            // 
            // tabAiPayload
            // 
            tabAiPayload.Controls.Add(pnlAiPayloadActions);
            tabAiPayload.Controls.Add(txtAiPayload);
            tabAiPayload.Location = new Point(4, 26);
            tabAiPayload.Name = "tabAiPayload";
            tabAiPayload.Padding = new Padding(3);
            tabAiPayload.Size = new Size(713, 387);
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
            pnlAiPayloadActions.Padding = new Padding(2);
            pnlAiPayloadActions.Size = new Size(707, 36);
            pnlAiPayloadActions.TabIndex = 0;
            // 
            // btnCopyAiPayload
            // 
            btnCopyAiPayload.Location = new Point(616, 5);
            btnCopyAiPayload.Name = "btnCopyAiPayload";
            btnCopyAiPayload.Size = new Size(84, 28);
            btnCopyAiPayload.TabIndex = 1;
            btnCopyAiPayload.Text = "📋 复制全部";
            btnCopyAiPayload.Click += btnCopyAiPayload_Click;
            // 
            // btnOpenAiPanel
            // 
            btnOpenAiPanel.Location = new Point(490, 5);
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
            txtAiPayload.Location = new Point(3, 3);
            txtAiPayload.Multiline = true;
            txtAiPayload.Name = "txtAiPayload";
            txtAiPayload.ReadOnly = true;
            txtAiPayload.ScrollBars = ScrollBars.Both;
            txtAiPayload.Size = new Size(707, 381);
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
            tabParseResult.Size = new Size(713, 387);
            tabParseResult.TabIndex = 3;
            tabParseResult.Text = "📥 解析AI结果";
            tabParseResult.UseVisualStyleBackColor = true;
            // 
            // txtAiResultInput
            // 
            txtAiResultInput.Dock = DockStyle.Fill;
            txtAiResultInput.Font = new Font("Consolas", 9F);
            txtAiResultInput.Location = new Point(3, 79);
            txtAiResultInput.Multiline = true;
            txtAiResultInput.Name = "txtAiResultInput";
            txtAiResultInput.ScrollBars = ScrollBars.Both;
            txtAiResultInput.Size = new Size(707, 305);
            txtAiResultInput.TabIndex = 2;
            txtAiResultInput.WordWrap = false;
            // 
            // lblParseHint
            // 
            lblParseHint.Dock = DockStyle.Top;
            lblParseHint.Location = new Point(3, 39);
            lblParseHint.Name = "lblParseHint";
            lblParseHint.Padding = new Padding(4, 3, 4, 3);
            lblParseHint.Size = new Size(707, 40);
            lblParseHint.TabIndex = 1;
            lblParseHint.Text = "将 AI 返回的 JSON 结果粘贴到下方（可包含 Markdown 代码块），支持中文 type 与常见字段别名。\r\n点击「解析并填充」后，整理建议与文件打标将自动生成，无需 AI API；可点「示例」查看格式。";
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
            pnlParseActions.Padding = new Padding(2);
            pnlParseActions.Size = new Size(707, 36);
            pnlParseActions.TabIndex = 0;
            // 
            // btnParseAiResult
            // 
            btnParseAiResult.Location = new Point(606, 5);
            btnParseAiResult.Name = "btnParseAiResult";
            btnParseAiResult.Size = new Size(94, 28);
            btnParseAiResult.TabIndex = 0;
            btnParseAiResult.Text = "🔍 解析并填充";
            btnParseAiResult.Click += btnParseAiResult_Click;
            // 
            // btnClearParseInput
            // 
            btnClearParseInput.Location = new Point(522, 5);
            btnClearParseInput.Name = "btnClearParseInput";
            btnClearParseInput.Size = new Size(78, 28);
            btnClearParseInput.TabIndex = 1;
            btnClearParseInput.Text = "\U0001f9f9 清空";
            btnClearParseInput.Click += btnClearParseInput_Click;
            // 
            // btnSampleJson
            // 
            btnSampleJson.Location = new Point(438, 5);
            btnSampleJson.Name = "btnSampleJson";
            btnSampleJson.Size = new Size(78, 28);
            btnSampleJson.TabIndex = 2;
            btnSampleJson.Text = "📄 示例";
            btnSampleJson.Click += btnSampleJson_Click;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(11, 482);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1466, 134);
            txtLog.TabIndex = 2;
            // 
            // BaiduPanAnalysisForm
            // 
            ClientSize = new Size(1488, 627);
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
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            pnlSummaryActions.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabRecommendations.ResumeLayout(false);
            tabTags.ResumeLayout(false);
            pnlTagActions.ResumeLayout(false);
            pnlTagFilter.ResumeLayout(false);
            pnlTagFilter.PerformLayout();
            tabAiPayload.ResumeLayout(false);
            tabAiPayload.PerformLayout();
            pnlAiPayloadActions.ResumeLayout(false);
            tabParseResult.ResumeLayout(false);
            tabParseResult.PerformLayout();
            pnlParseActions.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnPullSnapshot;
        private System.Windows.Forms.Button btnAIAnalysis;
        private System.Windows.Forms.Button btnClearSnapshot;
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
