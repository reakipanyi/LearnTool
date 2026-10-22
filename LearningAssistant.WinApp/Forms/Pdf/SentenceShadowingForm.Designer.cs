using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class SentenceShadowingForm
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        header = new Panel();
        _labelTitle = new Label();
        _labelInfo = new Label();
        _progressTrack = new Panel();
        body = new TableLayoutPanel();
        listCard = new SidebarCard();
        _listSentences = new ListBox();
        right = new TableLayoutPanel();
        currentCard = new SidebarCard();
        _labelCurrent = new Label();
        navigation = new FlowLayoutPanel();
        btnPrev = new Button();
        _btnPlay = new Button();
        btnNext = new Button();
        btnStop = new Button();
        practiceCard = new SidebarCard();
        grid = new TableLayoutPanel();
        _circularStatus = new CircularStatus();
        actions = new TableLayoutPanel();
        _btnRecord = new Button();
        _btnReplayRecording = new Button();
        _labelStatus = new Label();
        scoreCard = new SidebarCard();
        note = new Label();
        _labelScore = new Label();
        footer = new Panel();
        _comboMode = new ComboBox();
        hint = new Label();
        root.SuspendLayout();
        header.SuspendLayout();
        body.SuspendLayout();
        right.SuspendLayout();
        navigation.SuspendLayout();
        grid.SuspendLayout();
        actions.SuspendLayout();
        footer.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_progressTrack, 0, 1);
        root.Controls.Add(body, 0, 2);
        root.Controls.Add(footer, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Name = "root";
        root.Padding = new Padding(16, 8, 16, 10);
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        root.Size = new Size(860, 640);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.BackColor = Color.FromArgb(247, 248, 251);
        header.Controls.Add(_labelTitle);
        header.Controls.Add(_labelInfo);
        header.Dock = DockStyle.Fill;
        header.Location = new Point(16, 8);
        header.Name = "header";
        header.Size = new Size(828, 64);
        header.TabIndex = 0;
        // 
        // _labelTitle
        // 
        _labelTitle.AutoSize = true;
        _labelTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
        _labelTitle.ForeColor = Color.FromArgb(15, 23, 42);
        _labelTitle.Location = new Point(0, 2);
        _labelTitle.Name = "_labelTitle";
        _labelTitle.Size = new Size(86, 26);
        _labelTitle.TabIndex = 0;
        _labelTitle.Text = "句子跟读";
        // 
        // _labelInfo
        // 
        _labelInfo.AutoSize = true;
        _labelInfo.Font = new Font("Microsoft YaHei UI", 9F);
        _labelInfo.ForeColor = Color.FromArgb(100, 116, 139);
        _labelInfo.Location = new Point(2, 32);
        _labelInfo.Name = "_labelInfo";
        _labelInfo.Size = new Size(56, 17);
        _labelInfo.TabIndex = 1;
        _labelInfo.Tag = "muted";
        _labelInfo.Text = "第 1 页";
        // 
        // _progressTrack
        // 
        _progressTrack.BackColor = Color.FromArgb(247, 248, 251);
        _progressTrack.Dock = DockStyle.Fill;
        _progressTrack.Location = new Point(16, 72);
        _progressTrack.Margin = Padding.Empty;
        _progressTrack.Name = "_progressTrack";
        _progressTrack.Size = new Size(828, 8);
        _progressTrack.TabIndex = 1;
        // 
        // body
        // 
        body.BackColor = Color.FromArgb(247, 248, 251);
        body.ColumnCount = 2;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330F));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        body.Controls.Add(listCard, 0, 0);
        body.Controls.Add(right, 1, 0);
        body.Dock = DockStyle.Fill;
        body.Location = new Point(16, 80);
        body.Name = "body";
        body.RowCount = 1;
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.Size = new Size(828, 504);
        body.TabIndex = 2;
        // 
        // listCard
        // 
        listCard.BackColor = Color.FromArgb(247, 248, 251);
        listCard.Dock = DockStyle.Fill;
        listCard.Location = new Point(0, 0);
        listCard.Margin = new Padding(0, 0, 10, 0);
        listCard.Name = "listCard";
        listCard.Size = new Size(320, 504);
        listCard.TabIndex = 0;
        listCard.Title = "句子列表";
        // 
        // _listSentences
        // 
        _listSentences.BackColor = Color.White;
        _listSentences.BorderStyle = BorderStyle.None;
        _listSentences.DrawMode = DrawMode.OwnerDrawFixed;
        _listSentences.Dock = DockStyle.Fill;
        _listSentences.Font = new Font("Microsoft YaHei UI", 11.5F);
        _listSentences.ForeColor = Color.FromArgb(15, 23, 42);
        _listSentences.IntegralHeight = false;
        _listSentences.ItemHeight = 76;
        _listSentences.Location = new Point(10, 30);
        _listSentences.Name = "_listSentences";
        _listSentences.Size = new Size(300, 464);
        _listSentences.TabIndex = 0;
        // 
        // right
        // 
        right.BackColor = Color.FromArgb(247, 248, 251);
        right.ColumnCount = 1;
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        right.Controls.Add(currentCard, 0, 0);
        right.Controls.Add(practiceCard, 0, 1);
        right.Controls.Add(scoreCard, 0, 2);
        right.Dock = DockStyle.Fill;
        right.Location = new Point(330, 0);
        right.Name = "right";
        right.RowCount = 3;
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 24F));
        right.Size = new Size(498, 504);
        right.TabIndex = 1;
        // 
        // currentCard
        // 
        currentCard.BackColor = Color.FromArgb(247, 248, 251);
        currentCard.Dock = DockStyle.Fill;
        currentCard.Location = new Point(0, 0);
        currentCard.Margin = new Padding(0, 0, 0, 8);
        currentCard.Name = "currentCard";
        currentCard.Size = new Size(498, 173);
        currentCard.TabIndex = 0;
        currentCard.Title = "当前句";
        // 
        // _labelCurrent
        // 
        _labelCurrent.AutoEllipsis = false;
        _labelCurrent.AutoSize = false;
        _labelCurrent.Dock = DockStyle.Fill;
        _labelCurrent.Font = new Font("Microsoft YaHei UI", 13F);
        _labelCurrent.ForeColor = Color.FromArgb(15, 23, 42);
        _labelCurrent.Location = new Point(10, 30);
        _labelCurrent.Name = "_labelCurrent";
        _labelCurrent.Padding = new Padding(4, 2, 4, 2);
        _labelCurrent.Size = new Size(478, 85);
        _labelCurrent.TabIndex = 0;
        _labelCurrent.Text = "请从左侧选择一句开始跟读";
        _labelCurrent.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // navigation
        // 
        navigation.BackColor = Color.White;
        navigation.Controls.Add(btnPrev);
        navigation.Controls.Add(_btnPlay);
        navigation.Controls.Add(btnNext);
        navigation.Controls.Add(btnStop);
        navigation.Dock = DockStyle.Bottom;
        navigation.FlowDirection = FlowDirection.LeftToRight;
        navigation.Location = new Point(10, 115);
        navigation.Name = "navigation";
        navigation.Padding = new Padding(0, 4, 0, 0);
        navigation.Size = new Size(478, 44);
        navigation.TabIndex = 1;
        navigation.WrapContents = false;
        // 
        // btnPrev
        // 
        btnPrev.BackColor = Color.White;
        btnPrev.Cursor = Cursors.Hand;
        btnPrev.FlatAppearance.BorderSize = 0;
        btnPrev.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        btnPrev.FlatStyle = FlatStyle.Flat;
        btnPrev.Font = new Font("Segoe UI Emoji", 10F);
        btnPrev.ForeColor = Color.FromArgb(15, 23, 42);
        btnPrev.Location = new Point(0, 4);
        btnPrev.Margin = new Padding(0, 0, 6, 0);
        btnPrev.Name = "btnPrev";
        btnPrev.Size = new Size(38, 34);
        btnPrev.TabIndex = 0;
        btnPrev.Text = "◀";
        btnPrev.UseVisualStyleBackColor = false;
        // 
        // _btnPlay
        // 
        _btnPlay.BackColor = Color.FromArgb(108, 92, 231);
        _btnPlay.Cursor = Cursors.Hand;
        _btnPlay.FlatAppearance.BorderSize = 0;
        _btnPlay.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 58, 185);
        _btnPlay.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 72, 211);
        _btnPlay.FlatStyle = FlatStyle.Flat;
        _btnPlay.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        _btnPlay.ForeColor = Color.White;
        _btnPlay.Location = new Point(44, 4);
        _btnPlay.Margin = new Padding(0, 0, 6, 0);
        _btnPlay.Name = "_btnPlay";
        _btnPlay.Size = new Size(96, 34);
        _btnPlay.TabIndex = 1;
        _btnPlay.Tag = "accent";
        _btnPlay.Text = "▶ 播放";
        _btnPlay.UseVisualStyleBackColor = false;
        // 
        // btnNext
        // 
        btnNext.BackColor = Color.White;
        btnNext.Cursor = Cursors.Hand;
        btnNext.FlatAppearance.BorderSize = 0;
        btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        btnNext.FlatStyle = FlatStyle.Flat;
        btnNext.Font = new Font("Segoe UI Emoji", 10F);
        btnNext.ForeColor = Color.FromArgb(15, 23, 42);
        btnNext.Location = new Point(146, 4);
        btnNext.Margin = new Padding(0, 0, 6, 0);
        btnNext.Name = "btnNext";
        btnNext.Size = new Size(38, 34);
        btnNext.TabIndex = 2;
        btnNext.Text = "▶";
        btnNext.UseVisualStyleBackColor = false;
        // 
        // btnStop
        // 
        btnStop.BackColor = Color.White;
        btnStop.Cursor = Cursors.Hand;
        btnStop.FlatAppearance.BorderSize = 0;
        btnStop.FlatStyle = FlatStyle.Flat;
        btnStop.Font = new Font("Microsoft YaHei UI", 9F);
        btnStop.ForeColor = Color.FromArgb(100, 116, 139);
        btnStop.Location = new Point(190, 4);
        btnStop.Margin = Padding.Empty;
        btnStop.Name = "btnStop";
        btnStop.Size = new Size(64, 34);
        btnStop.TabIndex = 3;
        btnStop.Text = "停止";
        btnStop.UseVisualStyleBackColor = false;
        // 
        // practiceCard
        // 
        practiceCard.BackColor = Color.FromArgb(247, 248, 251);
        practiceCard.Dock = DockStyle.Fill;
        practiceCard.Location = new Point(0, 181);
        practiceCard.Margin = new Padding(0, 0, 0, 8);
        practiceCard.Name = "practiceCard";
        practiceCard.Size = new Size(498, 193);
        practiceCard.TabIndex = 1;
        practiceCard.Title = "跟读";
        // 
        // grid
        // 
        grid.BackColor = Color.White;
        grid.ColumnCount = 2;
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        grid.Controls.Add(_circularStatus, 0, 0);
        grid.Controls.Add(actions, 1, 0);
        grid.Dock = DockStyle.Fill;
        grid.Location = new Point(10, 30);
        grid.Name = "grid";
        grid.RowCount = 1;
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        grid.Size = new Size(478, 153);
        grid.TabIndex = 0;
        // 
        // _circularStatus
        // 
        _circularStatus.Anchor = AnchorStyles.None;
        _circularStatus.Location = new Point(12, 32);
        _circularStatus.Margin = new Padding(0, 4, 0, 0);
        _circularStatus.Name = "_circularStatus";
        _circularStatus.Size = new Size(88, 88);
        _circularStatus.TabIndex = 0;
        // 
        // actions
        // 
        actions.BackColor = Color.White;
        actions.ColumnCount = 1;
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actions.Controls.Add(_btnRecord, 0, 0);
        actions.Controls.Add(_btnReplayRecording, 0, 1);
        actions.Controls.Add(_labelStatus, 0, 2);
        actions.Dock = DockStyle.Fill;
        actions.Location = new Point(112, 0);
        actions.Name = "actions";
        actions.RowCount = 3;
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actions.Size = new Size(366, 153);
        actions.TabIndex = 1;
        // 
        // _btnRecord
        // 
        _btnRecord.BackColor = Color.FromArgb(239, 68, 68);
        _btnRecord.Cursor = Cursors.Hand;
        _btnRecord.Dock = DockStyle.Fill;
        _btnRecord.FlatAppearance.BorderSize = 0;
        _btnRecord.FlatStyle = FlatStyle.Flat;
        _btnRecord.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _btnRecord.ForeColor = Color.White;
        _btnRecord.Location = new Point(0, 2);
        _btnRecord.Margin = new Padding(0, 2, 0, 4);
        _btnRecord.Name = "_btnRecord";
        _btnRecord.Size = new Size(366, 40);
        _btnRecord.TabIndex = 0;
        _btnRecord.Tag = "record";
        _btnRecord.Text = "🎤 录音";
        _btnRecord.UseVisualStyleBackColor = false;
        // 
        // _btnReplayRecording
        // 
        _btnReplayRecording.BackColor = Color.White;
        _btnReplayRecording.Cursor = Cursors.Hand;
        _btnReplayRecording.Dock = DockStyle.Fill;
        _btnReplayRecording.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnReplayRecording.FlatAppearance.BorderSize = 1;
        _btnReplayRecording.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnReplayRecording.FlatStyle = FlatStyle.Flat;
        _btnReplayRecording.ForeColor = Color.FromArgb(15, 23, 42);
        _btnReplayRecording.Location = new Point(0, 46);
        _btnReplayRecording.Margin = new Padding(0, 0, 0, 4);
        _btnReplayRecording.Name = "_btnReplayRecording";
        _btnReplayRecording.Size = new Size(366, 34);
        _btnReplayRecording.TabIndex = 1;
        _btnReplayRecording.Text = "▶ 回放我的录音";
        _btnReplayRecording.UseVisualStyleBackColor = false;
        // 
        // _labelStatus
        // 
        _labelStatus.AutoSize = false;
        _labelStatus.Dock = DockStyle.Fill;
        _labelStatus.Font = new Font("Microsoft YaHei UI", 8.5F);
        _labelStatus.ForeColor = Color.FromArgb(100, 116, 139);
        _labelStatus.Location = new Point(0, 84);
        _labelStatus.Name = "_labelStatus";
        _labelStatus.Padding = new Padding(2, 2, 2, 0);
        _labelStatus.Size = new Size(366, 69);
        _labelStatus.TabIndex = 2;
        _labelStatus.Tag = "muted";
        // 
        // scoreCard
        // 
        scoreCard.BackColor = Color.FromArgb(247, 248, 251);
        scoreCard.Dock = DockStyle.Fill;
        scoreCard.Location = new Point(0, 382);
        scoreCard.Margin = Padding.Empty;
        scoreCard.Name = "scoreCard";
        scoreCard.Size = new Size(498, 122);
        scoreCard.TabIndex = 2;
        scoreCard.Title = "参考分";
        // 
        // note
        // 
        note.AutoSize = false;
        note.Dock = DockStyle.Top;
        note.Font = new Font("Microsoft YaHei UI", 8.5F);
        note.ForeColor = Color.FromArgb(100, 116, 139);
        note.Location = new Point(10, 64);
        note.Name = "note";
        note.Size = new Size(478, 24);
        note.TabIndex = 0;
        note.Tag = "muted";
        note.Text = "仅反映跟读完成度，不作为发音准确率结论。";
        // 
        // _labelScore
        // 
        _labelScore.AutoSize = false;
        _labelScore.Dock = DockStyle.Top;
        _labelScore.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
        _labelScore.ForeColor = Color.FromArgb(139, 92, 246);
        _labelScore.Location = new Point(10, 30);
        _labelScore.Name = "_labelScore";
        _labelScore.Size = new Size(478, 34);
        _labelScore.TabIndex = 1;
        _labelScore.Tag = "score";
        _labelScore.Text = "参考分：—";
        _labelScore.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // footer
        // 
        footer.BackColor = Color.FromArgb(247, 248, 251);
        footer.Controls.Add(_comboMode);
        footer.Controls.Add(hint);
        footer.Dock = DockStyle.Fill;
        footer.Location = new Point(16, 584);
        footer.Name = "footer";
        footer.Size = new Size(828, 46);
        footer.TabIndex = 3;
        // 
        // _comboMode
        // 
        _comboMode.BackColor = Color.White;
        _comboMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _comboMode.FlatStyle = FlatStyle.Flat;
        _comboMode.Font = new Font("Microsoft YaHei UI", 9F);
        _comboMode.ForeColor = Color.FromArgb(15, 23, 42);
        _comboMode.Location = new Point(0, 10);
        _comboMode.Name = "_comboMode";
        _comboMode.Size = new Size(124, 25);
        _comboMode.TabIndex = 0;
        // 
        // hint
        // 
        hint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        hint.AutoSize = false;
        hint.Font = new Font("Microsoft YaHei UI", 8.5F);
        hint.ForeColor = Color.FromArgb(100, 116, 139);
        hint.Location = new Point(588, 10);
        hint.Name = "hint";
        hint.Size = new Size(240, 28);
        hint.TabIndex = 1;
        hint.Tag = "muted";
        hint.Text = "空格 播放 · R 重播 · → 下一句";
        hint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // SentenceShadowingForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        ClientSize = new Size(860, 640);
        Controls.Add(root);
        Font = new Font("Microsoft YaHei UI", 9.5F);
        KeyPreview = true;
        MinimumSize = new Size(720, 560);
        Name = "SentenceShadowingForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "句子跟读";
        root.ResumeLayout(false);
        root.PerformLayout();
        header.ResumeLayout(false);
        header.PerformLayout();
        body.ResumeLayout(false);
        right.ResumeLayout(false);
        navigation.ResumeLayout(false);
        grid.ResumeLayout(false);
        actions.ResumeLayout(false);
        footer.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private Panel header;
    private Label _labelTitle;
    private Label _labelInfo;
    private Panel _progressTrack;
    private TableLayoutPanel body;
    private SidebarCard listCard;
    private ListBox _listSentences;
    private TableLayoutPanel right;
    private SidebarCard currentCard;
    private Label _labelCurrent;
    private FlowLayoutPanel navigation;
    private Button btnPrev;
    private Button _btnPlay;
    private Button btnNext;
    private Button btnStop;
    private SidebarCard practiceCard;
    private TableLayoutPanel grid;
    private CircularStatus _circularStatus;
    private TableLayoutPanel actions;
    private Button _btnRecord;
    private Button _btnReplayRecording;
    private Label _labelStatus;
    private SidebarCard scoreCard;
    private Label note;
    private Label _labelScore;
    private Panel footer;
    private ComboBox _comboMode;
    private Label hint;
}
