namespace LearningAssistant.Forms.Pdf;

partial class PdfRecordingPanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        statusLabel = new Label();
        buttonRow = new TableLayoutPanel();
        recordButton = new Button();
        stopButton = new Button();
        playButton = new Button();
        timeLabel = new Label();
        positionBar = new TrackBar();
        levelMeter = new Panel();
        levelFill = new Panel();
        listTitle = new Label();
        noRecordingsLabel = new Label();
        recordingsList = new ListBox();
        actionsPanel = new Panel();
        deleteButton = new Button();
        generateSummaryButton = new Button();
        timestampsTitle = new Label();
        noTimestampsLabel = new Label();
        timestampsList = new ListBox();
        summariesTitle = new Label();
        noSummariesLabel = new Label();
        summariesList = new ListBox();
        root.SuspendLayout();
        buttonRow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)positionBar).BeginInit();
        levelMeter.SuspendLayout();
        actionsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.AutoSize = true;
        root.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(statusLabel, 0, 0);
        root.Controls.Add(buttonRow, 0, 1);
        root.Controls.Add(timeLabel, 0, 2);
        root.Controls.Add(positionBar, 0, 3);
        root.Controls.Add(levelMeter, 0, 4);
        root.Controls.Add(listTitle, 0, 5);
        root.Controls.Add(noRecordingsLabel, 0, 6);
        root.Controls.Add(recordingsList, 0, 6);
        root.Controls.Add(actionsPanel, 0, 7);
        root.Controls.Add(timestampsTitle, 0, 8);
        root.Controls.Add(noTimestampsLabel, 0, 9);
        root.Controls.Add(timestampsList, 0, 9);
        root.Controls.Add(summariesTitle, 0, 10);
        root.Controls.Add(noSummariesLabel, 0, 11);
        root.Controls.Add(summariesList, 0, 11);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.Padding = new Padding(12, 10, 12, 10);
        root.RowCount = 12;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 27F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
        root.Size = new Size(555, 900);
        root.TabIndex = 0;
        // 
        // statusLabel
        // 
        statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        statusLabel.AutoSize = true;
        statusLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        statusLabel.ForeColor = Color.FromArgb(50, 50, 60);
        statusLabel.Location = new Point(12, 10);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(32, 17);
        statusLabel.TabIndex = 0;
        statusLabel.Text = "就绪";
        // 
        // buttonRow
        // 
        buttonRow.ColumnCount = 3;
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        buttonRow.Controls.Add(recordButton, 0, 0);
        buttonRow.Controls.Add(stopButton, 1, 0);
        buttonRow.Controls.Add(playButton, 2, 0);
        buttonRow.Dock = DockStyle.Fill;
        buttonRow.Location = new Point(12, 34);
        buttonRow.Margin = new Padding(0);
        buttonRow.Name = "buttonRow";
        buttonRow.RowCount = 1;
        buttonRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        buttonRow.Size = new Size(531, 36);
        buttonRow.TabIndex = 1;
        // 
        // recordButton
        // 
        recordButton.BackColor = Color.FromArgb(244, 67, 54);
        recordButton.Cursor = Cursors.Hand;
        recordButton.Dock = DockStyle.Fill;
        recordButton.FlatAppearance.BorderSize = 0;
        recordButton.FlatStyle = FlatStyle.Flat;
        recordButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        recordButton.ForeColor = Color.White;
        recordButton.Location = new Point(0, 0);
        recordButton.Margin = new Padding(0, 0, 3, 0);
        recordButton.Name = "recordButton";
        recordButton.Size = new Size(171, 36);
        recordButton.TabIndex = 0;
        recordButton.Tag = "record";
        recordButton.Text = "● 录制";
        recordButton.UseVisualStyleBackColor = false;
        // 
        // stopButton
        // 
        stopButton.BackColor = Color.FromArgb(100, 100, 100);
        stopButton.Cursor = Cursors.Hand;
        stopButton.Dock = DockStyle.Fill;
        stopButton.Enabled = false;
        stopButton.FlatAppearance.BorderSize = 0;
        stopButton.FlatStyle = FlatStyle.Flat;
        stopButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        stopButton.ForeColor = Color.White;
        stopButton.Location = new Point(177, 0);
        stopButton.Margin = new Padding(3, 0, 3, 0);
        stopButton.Name = "stopButton";
        stopButton.Size = new Size(171, 36);
        stopButton.TabIndex = 1;
        stopButton.Tag = "stop";
        stopButton.Text = "■ 停止";
        stopButton.UseVisualStyleBackColor = false;
        // 
        // playButton
        // 
        playButton.BackColor = Color.FromArgb(25, 118, 210);
        playButton.Cursor = Cursors.Hand;
        playButton.Dock = DockStyle.Fill;
        playButton.Enabled = false;
        playButton.FlatAppearance.BorderSize = 0;
        playButton.FlatStyle = FlatStyle.Flat;
        playButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        playButton.ForeColor = Color.White;
        playButton.Location = new Point(354, 0);
        playButton.Margin = new Padding(3, 0, 0, 0);
        playButton.Name = "playButton";
        playButton.Size = new Size(177, 36);
        playButton.TabIndex = 2;
        playButton.Tag = "play";
        playButton.Text = "▶ 播放";
        playButton.UseVisualStyleBackColor = false;
        // 
        // timeLabel
        // 
        timeLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        timeLabel.Font = new Font("Consolas", 9F);
        timeLabel.ForeColor = Color.FromArgb(50, 50, 60);
        timeLabel.Location = new Point(12, 70);
        timeLabel.Name = "timeLabel";
        timeLabel.Size = new Size(531, 20);
        timeLabel.TabIndex = 2;
        timeLabel.Text = "00:00 / 00:00";
        timeLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // positionBar
        // 
        positionBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        positionBar.Location = new Point(12, 94);
        positionBar.Maximum = 1000;
        positionBar.Name = "positionBar";
        positionBar.Size = new Size(531, 30);
        positionBar.TabIndex = 3;
        positionBar.TickStyle = TickStyle.None;
        // 
        // levelMeter
        // 
        levelMeter.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        levelMeter.BackColor = Color.FromArgb(220, 220, 230);
        levelMeter.Controls.Add(levelFill);
        levelMeter.Location = new Point(12, 128);
        levelMeter.Name = "levelMeter";
        levelMeter.Size = new Size(531, 12);
        levelMeter.TabIndex = 4;
        // 
        // levelFill
        // 
        levelFill.BackColor = Color.FromArgb(25, 118, 210);
        levelFill.Location = new Point(1, 1);
        levelFill.Name = "levelFill";
        levelFill.Size = new Size(0, 10);
        levelFill.TabIndex = 0;
        // 
        // listTitle
        // 
        listTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        listTitle.AutoSize = true;
        listTitle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        listTitle.ForeColor = Color.FromArgb(50, 50, 60);
        listTitle.Location = new Point(12, 144);
        listTitle.Name = "listTitle";
        listTitle.Size = new Size(60, 17);
        listTitle.TabIndex = 5;
        listTitle.Text = "录音列表";
        // 
        // noRecordingsLabel
        // 
        noRecordingsLabel.Dock = DockStyle.Fill;
        noRecordingsLabel.Font = new Font("Microsoft YaHei UI", 9F);
        noRecordingsLabel.ForeColor = Color.FromArgb(160, 160, 170);
        noRecordingsLabel.Location = new Point(12, 166);
        noRecordingsLabel.Name = "noRecordingsLabel";
        noRecordingsLabel.Size = new Size(531, 347);
        noRecordingsLabel.TabIndex = 6;
        noRecordingsLabel.Text = "暂无录音，点击 ● 录制 开始";
        noRecordingsLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // recordingsList
        // 
        recordingsList.BorderStyle = BorderStyle.FixedSingle;
        recordingsList.Dock = DockStyle.Fill;
        recordingsList.DrawMode = DrawMode.OwnerDrawFixed;
        recordingsList.IntegralHeight = false;
        recordingsList.ItemHeight = 36;
        recordingsList.Location = new Point(12, 166);
        recordingsList.Name = "recordingsList";
        recordingsList.Size = new Size(531, 347);
        recordingsList.TabIndex = 7;
        recordingsList.Visible = false;
        // 
        // actionsPanel
        // 
        actionsPanel.Controls.Add(deleteButton);
        actionsPanel.Controls.Add(generateSummaryButton);
        actionsPanel.Dock = DockStyle.Fill;
        actionsPanel.Location = new Point(12, 513);
        actionsPanel.Margin = new Padding(0);
        actionsPanel.Name = "actionsPanel";
        actionsPanel.Size = new Size(531, 30);
        actionsPanel.TabIndex = 8;
        // 
        // deleteButton
        // 
        deleteButton.BackColor = Color.FromArgb(230, 230, 235);
        deleteButton.Cursor = Cursors.Hand;
        deleteButton.FlatAppearance.BorderSize = 0;
        deleteButton.FlatStyle = FlatStyle.Flat;
        deleteButton.Font = new Font("Microsoft YaHei UI", 9F);
        deleteButton.ForeColor = Color.FromArgb(50, 50, 60);
        deleteButton.Location = new Point(0, 2);
        deleteButton.Name = "deleteButton";
        deleteButton.Size = new Size(60, 26);
        deleteButton.TabIndex = 0;
        deleteButton.Tag = "delete";
        deleteButton.Text = "删除";
        deleteButton.UseVisualStyleBackColor = false;
        // 
        // generateSummaryButton
        // 
        generateSummaryButton.BackColor = Color.FromArgb(25, 118, 210);
        generateSummaryButton.Cursor = Cursors.Hand;
        generateSummaryButton.FlatAppearance.BorderSize = 0;
        generateSummaryButton.FlatStyle = FlatStyle.Flat;
        generateSummaryButton.Font = new Font("Microsoft YaHei UI", 9F);
        generateSummaryButton.ForeColor = Color.White;
        generateSummaryButton.Location = new Point(66, 2);
        generateSummaryButton.Name = "generateSummaryButton";
        generateSummaryButton.Size = new Size(72, 26);
        generateSummaryButton.TabIndex = 1;
        generateSummaryButton.Text = "AI 摘要";
        generateSummaryButton.UseVisualStyleBackColor = false;
        // 
        // timestampsTitle
        // 
        timestampsTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        timestampsTitle.AutoSize = true;
        timestampsTitle.Cursor = Cursors.Hand;
        timestampsTitle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        timestampsTitle.ForeColor = Color.FromArgb(50, 50, 60);
        timestampsTitle.Location = new Point(12, 543);
        timestampsTitle.Name = "timestampsTitle";
        timestampsTitle.Size = new Size(180, 17);
        timestampsTitle.TabIndex = 9;
        timestampsTitle.Text = "本页时间戳标注（点击跳转回放）";
        // 
        // noTimestampsLabel
        // 
        noTimestampsLabel.Dock = DockStyle.Fill;
        noTimestampsLabel.Font = new Font("Microsoft YaHei UI", 9F);
        noTimestampsLabel.ForeColor = Color.FromArgb(160, 160, 170);
        noTimestampsLabel.Location = new Point(12, 565);
        noTimestampsLabel.Name = "noTimestampsLabel";
        noTimestampsLabel.Size = new Size(531, 1);
        noTimestampsLabel.TabIndex = 10;
        noTimestampsLabel.Text = "本页暂无时间戳标注";
        noTimestampsLabel.TextAlign = ContentAlignment.MiddleCenter;
        noTimestampsLabel.Visible = false;
        // 
        // timestampsList
        // 
        timestampsList.BorderStyle = BorderStyle.FixedSingle;
        timestampsList.Dock = DockStyle.Fill;
        timestampsList.DrawMode = DrawMode.OwnerDrawFixed;
        timestampsList.IntegralHeight = false;
        timestampsList.ItemHeight = 32;
        timestampsList.Location = new Point(12, 565);
        timestampsList.Name = "timestampsList";
        timestampsList.Size = new Size(531, 1);
        timestampsList.TabIndex = 11;
        timestampsList.Visible = false;
        // 
        // summariesTitle
        // 
        summariesTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        summariesTitle.AutoSize = true;
        summariesTitle.Cursor = Cursors.Hand;
        summariesTitle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        summariesTitle.ForeColor = Color.FromArgb(50, 50, 60);
        summariesTitle.Location = new Point(12, 566);
        summariesTitle.Name = "summariesTitle";
        summariesTitle.Size = new Size(140, 17);
        summariesTitle.TabIndex = 12;
        summariesTitle.Text = "AI 摘要（点击跳页并回放）";
        // 
        // noSummariesLabel
        // 
        noSummariesLabel.Dock = DockStyle.Fill;
        noSummariesLabel.Font = new Font("Microsoft YaHei UI", 9F);
        noSummariesLabel.ForeColor = Color.FromArgb(160, 160, 170);
        noSummariesLabel.Location = new Point(12, 588);
        noSummariesLabel.Name = "noSummariesLabel";
        noSummariesLabel.Size = new Size(531, 1);
        noSummariesLabel.TabIndex = 13;
        noSummariesLabel.Text = "暂无 AI 摘要，点击「AI 摘要」生成";
        noSummariesLabel.TextAlign = ContentAlignment.MiddleCenter;
        noSummariesLabel.Visible = false;
        // 
        // summariesList
        // 
        summariesList.BorderStyle = BorderStyle.FixedSingle;
        summariesList.Dock = DockStyle.Fill;
        summariesList.DrawMode = DrawMode.OwnerDrawFixed;
        summariesList.IntegralHeight = false;
        summariesList.ItemHeight = 46;
        summariesList.Location = new Point(12, 588);
        summariesList.Name = "summariesList";
        summariesList.Size = new Size(531, 1);
        summariesList.TabIndex = 14;
        summariesList.Visible = false;
        // 
        // PdfRecordingPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfRecordingPanel";
        Size = new Size(555, 900);
        root.ResumeLayout(false);
        root.PerformLayout();
        buttonRow.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)positionBar).EndInit();
        levelMeter.ResumeLayout(false);
        actionsPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel root;
    private Label statusLabel;
    private TableLayoutPanel buttonRow;
    private Button recordButton;
    private Button stopButton;
    private Button playButton;
    private Label timeLabel;
    private TrackBar positionBar;
    private Panel levelMeter;
    private Panel levelFill;
    private Label listTitle;
    private Label noRecordingsLabel;
    private ListBox recordingsList;
    private Panel actionsPanel;
    private Button deleteButton;
    private Button generateSummaryButton;
    private Label timestampsTitle;
    private Label noTimestampsLabel;
    private ListBox timestampsList;
    private Label summariesTitle;
    private Label noSummariesLabel;
    private ListBox summariesList;
}
