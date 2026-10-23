using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfAudioPanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        statusPanel = new Panel();
        _labelInfo = new Label();
        listCard = new SidebarCard();
        _listTracks = new ListBox();
        playbackCard = new SidebarCard();
        playback = new TableLayoutPanel();
        transport = new FlowLayoutPanel();
        _btnPrev = new Button();
        _btnPlay = new Button();
        _btnNext = new Button();
        _btnLoop = new Button();
        abRow = new TableLayoutPanel();
        _btnSetA = new Button();
        _btnSetB = new Button();
        _btnClearAb = new Button();
        _labelLoop = new Label();
        speedRow = new TableLayoutPanel();
        speedLabel = new Label();
        _comboSpeed = new ComboBox();
        speedNote = new Label();
        _labelSubtitle = new Label();
        sourceCard = new SidebarCard();
        sourceGrid = new TableLayoutPanel();
        _btnAddDir = new Button();
        _btnAiMatch = new Button();
        _btnRules = new Button();
        _btnManual = new Button();
        _btnManualFromList = new Button();
        _btnRescan = new Button();
        _btnFollowRead = new Button();
        _chkSubDir = new CheckBox();
        _tooltip = new ToolTip(components);
        root.SuspendLayout();
        statusPanel.SuspendLayout();
        playback.SuspendLayout();
        transport.SuspendLayout();
        abRow.SuspendLayout();
        speedRow.SuspendLayout();
        sourceGrid.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(statusPanel, 0, 0);
        root.Controls.Add(listCard, 0, 1);
        root.Controls.Add(playbackCard, 0, 2);
        root.Controls.Add(sourceCard, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(6, 2);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 198F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 188F));
        root.Size = new Size(308, 624);
        root.TabIndex = 0;
        // 
        // statusPanel
        // 
        statusPanel.BackColor = Color.White;
        statusPanel.Controls.Add(_labelInfo);
        statusPanel.Dock = DockStyle.Fill;
        statusPanel.Location = new Point(0, 0);
        statusPanel.Margin = new Padding(0, 0, 0, 4);
        statusPanel.Name = "statusPanel";
        statusPanel.Padding = new Padding(10, 6, 10, 4);
        statusPanel.Size = new Size(308, 42);
        statusPanel.TabIndex = 0;
        statusPanel.Tag = "surface";
        // 
        // _labelInfo
        // 
        _labelInfo.AutoEllipsis = true;
        _labelInfo.Dock = DockStyle.Top;
        _labelInfo.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _labelInfo.ForeColor = Color.FromArgb(15, 23, 42);
        _labelInfo.Location = new Point(10, 6);
        _labelInfo.Name = "_labelInfo";
        _labelInfo.Size = new Size(288, 28);
        _labelInfo.TabIndex = 0;
        _labelInfo.Text = "本页：无音频";
        _labelInfo.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // listCard
        // 
        listCard.BackColor = Color.FromArgb(247, 248, 251);
        listCard.Dock = DockStyle.Fill;
        listCard.Location = new Point(0, 46);
        listCard.Margin = new Padding(0, 0, 0, 4);
        listCard.Name = "listCard";
        listCard.Padding = new Padding(2, 1, 2, 1);
        listCard.Size = new Size(308, 188);
        listCard.TabIndex = 1;
        listCard.Title = "曲目";
        // 
        // _listTracks
        // 
        _listTracks.BackColor = Color.White;
        _listTracks.BorderStyle = BorderStyle.None;
        _listTracks.Dock = DockStyle.Fill;
        _listTracks.DrawMode = DrawMode.OwnerDrawFixed;
        _listTracks.ForeColor = Color.FromArgb(15, 23, 42);
        _listTracks.IntegralHeight = false;
        _listTracks.ItemHeight = 30;
        _listTracks.Location = new Point(10, 30);
        _listTracks.Name = "_listTracks";
        _listTracks.Size = new Size(284, 146);
        _listTracks.TabIndex = 0;
        // 
        // playbackCard
        // 
        playbackCard.BackColor = Color.FromArgb(247, 248, 251);
        playbackCard.Dock = DockStyle.Fill;
        playbackCard.Location = new Point(0, 238);
        playbackCard.Margin = new Padding(0, 0, 0, 4);
        playbackCard.Name = "playbackCard";
        playbackCard.Padding = new Padding(2, 1, 2, 1);
        playbackCard.Size = new Size(308, 194);
        playbackCard.TabIndex = 2;
        playbackCard.Title = "播放";
        // 
        // playback
        // 
        playback.BackColor = Color.Transparent;
        playback.ColumnCount = 1;
        playback.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        playback.Controls.Add(transport, 0, 0);
        playback.Controls.Add(abRow, 0, 1);
        playback.Controls.Add(_labelLoop, 0, 2);
        playback.Controls.Add(speedRow, 0, 3);
        playback.Controls.Add(_labelSubtitle, 0, 4);
        playback.Dock = DockStyle.Fill;
        playback.Location = new Point(10, 30);
        playback.Margin = new Padding(0);
        playback.Name = "playback";
        playback.RowCount = 5;
        playback.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        playback.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        playback.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        playback.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        playback.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        playback.Size = new Size(284, 152);
        playback.TabIndex = 0;
        // 
        // transport
        // 
        transport.AutoSize = true;
        transport.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        transport.BackColor = Color.Transparent;
        transport.Controls.Add(_btnPrev);
        transport.Controls.Add(_btnPlay);
        transport.Controls.Add(_btnNext);
        transport.Controls.Add(_btnLoop);
        transport.Location = new Point(0, 0);
        transport.Margin = new Padding(0);
        transport.Name = "transport";
        transport.Size = new Size(132, 34);
        transport.TabIndex = 0;
        transport.WrapContents = false;
        // 
        // _btnPrev
        // 
        _btnPrev.BackColor = Color.White;
        _btnPrev.Cursor = Cursors.Hand;
        _btnPrev.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnPrev.FlatStyle = FlatStyle.Flat;
        _btnPrev.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _btnPrev.ForeColor = Color.FromArgb(15, 23, 42);
        _btnPrev.Location = new Point(3, 0);
        _btnPrev.Margin = new Padding(3, 0, 3, 0);
        _btnPrev.Name = "_btnPrev";
        _btnPrev.Size = new Size(34, 34);
        _btnPrev.TabIndex = 0;
        _btnPrev.Tag = "transport";
        _btnPrev.Text = "⏮";
        _btnPrev.UseVisualStyleBackColor = false;
        // 
        // _btnPlay
        // 
        _btnPlay.BackColor = Color.FromArgb(108, 92, 231);
        _btnPlay.Cursor = Cursors.Hand;
        _btnPlay.FlatAppearance.BorderSize = 0;
        _btnPlay.FlatStyle = FlatStyle.Flat;
        _btnPlay.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        _btnPlay.ForeColor = Color.White;
        _btnPlay.Location = new Point(43, 0);
        _btnPlay.Margin = new Padding(3, 0, 3, 0);
        _btnPlay.Name = "_btnPlay";
        _btnPlay.Size = new Size(46, 34);
        _btnPlay.TabIndex = 1;
        _btnPlay.Tag = "accent";
        _btnPlay.Text = "▶";
        _btnPlay.UseVisualStyleBackColor = false;
        // 
        // _btnNext
        // 
        _btnNext.BackColor = Color.White;
        _btnNext.Cursor = Cursors.Hand;
        _btnNext.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnNext.FlatStyle = FlatStyle.Flat;
        _btnNext.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _btnNext.ForeColor = Color.FromArgb(15, 23, 42);
        _btnNext.Location = new Point(95, 0);
        _btnNext.Margin = new Padding(3, 0, 3, 0);
        _btnNext.Name = "_btnNext";
        _btnNext.Size = new Size(34, 34);
        _btnNext.TabIndex = 2;
        _btnNext.Tag = "transport";
        _btnNext.Text = "⏭";
        _btnNext.UseVisualStyleBackColor = false;
        // 
        // abRow
        // 
        abRow.BackColor = Color.Transparent;
        abRow.ColumnCount = 3;
        abRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));
        abRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
        abRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
        abRow.Controls.Add(_btnSetA, 0, 0);
        abRow.Controls.Add(_btnSetB, 1, 0);
        abRow.Controls.Add(_btnClearAb, 2, 0);
        abRow.Dock = DockStyle.Fill;
        abRow.Location = new Point(0, 40);
        abRow.Margin = new Padding(0);
        abRow.Name = "abRow";
        abRow.RowCount = 1;
        abRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        abRow.Size = new Size(284, 36);
        abRow.TabIndex = 1;
        // 
        // _btnSetA
        // 
        _btnSetA.AutoEllipsis = true;
        _btnSetA.BackColor = Color.White;
        _btnSetA.Cursor = Cursors.Hand;
        _btnSetA.Dock = DockStyle.Fill;
        _btnSetA.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnSetA.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnSetA.FlatStyle = FlatStyle.Flat;
        _btnSetA.Font = new Font("Microsoft YaHei UI", 9F);
        _btnSetA.ForeColor = Color.FromArgb(15, 23, 42);
        _btnSetA.Location = new Point(0, 0);
        _btnSetA.Margin = new Padding(0, 0, 4, 0);
        _btnSetA.Name = "_btnSetA";
        _btnSetA.Size = new Size(90, 36);
        _btnSetA.TabIndex = 0;
        _btnSetA.Tag = "secondary";
        _btnSetA.Text = "设起点";
        _btnSetA.UseVisualStyleBackColor = false;
        // 
        // _btnSetB
        // 
        _btnSetB.AutoEllipsis = true;
        _btnSetB.BackColor = Color.White;
        _btnSetB.Cursor = Cursors.Hand;
        _btnSetB.Dock = DockStyle.Fill;
        _btnSetB.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnSetB.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnSetB.FlatStyle = FlatStyle.Flat;
        _btnSetB.Font = new Font("Microsoft YaHei UI", 9F);
        _btnSetB.ForeColor = Color.FromArgb(15, 23, 42);
        _btnSetB.Location = new Point(94, 0);
        _btnSetB.Margin = new Padding(0, 0, 4, 0);
        _btnSetB.Name = "_btnSetB";
        _btnSetB.Size = new Size(90, 36);
        _btnSetB.TabIndex = 1;
        _btnSetB.Tag = "secondary";
        _btnSetB.Text = "设终点";
        _btnSetB.UseVisualStyleBackColor = false;
        // 
        // _btnClearAb
        // 
        _btnClearAb.AutoEllipsis = true;
        _btnClearAb.BackColor = Color.White;
        _btnClearAb.Cursor = Cursors.Hand;
        _btnClearAb.Dock = DockStyle.Fill;
        _btnClearAb.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnClearAb.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnClearAb.FlatStyle = FlatStyle.Flat;
        _btnClearAb.Font = new Font("Microsoft YaHei UI", 9F);
        _btnClearAb.ForeColor = Color.FromArgb(15, 23, 42);
        _btnClearAb.Location = new Point(188, 0);
        _btnClearAb.Margin = new Padding(0);
        _btnClearAb.Name = "_btnClearAb";
        _btnClearAb.Size = new Size(96, 36);
        _btnClearAb.TabIndex = 2;
        _btnClearAb.Tag = "secondary";
        _btnClearAb.Text = "取消复读";
        _btnClearAb.UseVisualStyleBackColor = false;
        // 
        // _labelLoop
        // 
        _labelLoop.AutoEllipsis = true;
        _labelLoop.Dock = DockStyle.Fill;
        _labelLoop.Font = new Font("Microsoft YaHei UI", 8.5F);
        _labelLoop.ForeColor = Color.FromArgb(100, 116, 139);
        _labelLoop.Location = new Point(3, 76);
        _labelLoop.Name = "_labelLoop";
        _labelLoop.Size = new Size(278, 22);
        _labelLoop.TabIndex = 2;
        _labelLoop.Tag = "muted";
        _labelLoop.Text = "复读：未设置";
        _labelLoop.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // speedRow
        // 
        speedRow.BackColor = Color.Transparent;
        speedRow.ColumnCount = 3;
        speedRow.ColumnStyles.Add(new ColumnStyle());
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        speedRow.Controls.Add(speedLabel, 0, 0);
        speedRow.Controls.Add(_comboSpeed, 1, 0);
        speedRow.Controls.Add(speedNote, 2, 0);
        speedRow.Dock = DockStyle.Fill;
        speedRow.Location = new Point(0, 98);
        speedRow.Margin = new Padding(0);
        speedRow.Name = "speedRow";
        speedRow.RowCount = 1;
        speedRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        speedRow.Size = new Size(284, 32);
        speedRow.TabIndex = 3;
        // 
        // speedLabel
        // 
        speedLabel.Anchor = AnchorStyles.Left;
        speedLabel.AutoSize = true;
        speedLabel.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
        speedLabel.ForeColor = Color.FromArgb(100, 116, 139);
        speedLabel.Location = new Point(0, 7);
        speedLabel.Margin = new Padding(0, 0, 6, 0);
        speedLabel.Name = "speedLabel";
        speedLabel.Size = new Size(32, 17);
        speedLabel.TabIndex = 0;
        speedLabel.Tag = "muted";
        speedLabel.Text = "倍速";
        // 
        // _comboSpeed
        // 
        _comboSpeed.BackColor = Color.White;
        _comboSpeed.Dock = DockStyle.Fill;
        _comboSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
        _comboSpeed.Enabled = false;
        _comboSpeed.ForeColor = Color.FromArgb(15, 23, 42);
        _comboSpeed.Items.AddRange(new object[] { "1.0x（不变调）" });
        _comboSpeed.Location = new Point(38, 0);
        _comboSpeed.Margin = new Padding(0, 0, 6, 0);
        _comboSpeed.Name = "_comboSpeed";
        _comboSpeed.Size = new Size(112, 25);
        _comboSpeed.TabIndex = 1;
        // 
        // speedNote
        // 
        speedNote.AutoEllipsis = true;
        speedNote.Dock = DockStyle.Fill;
        speedNote.Font = new Font("Microsoft YaHei UI", 8.5F);
        speedNote.ForeColor = Color.FromArgb(100, 116, 139);
        speedNote.Location = new Point(156, 0);
        speedNote.Margin = new Padding(0);
        speedNote.Name = "speedNote";
        speedNote.Size = new Size(128, 32);
        speedNote.TabIndex = 2;
        speedNote.Tag = "muted";
        speedNote.Text = "引擎待评估";
        speedNote.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _labelSubtitle
        // 
        _labelSubtitle.AutoEllipsis = true;
        _labelSubtitle.Dock = DockStyle.Fill;
        _labelSubtitle.Font = new Font("Microsoft YaHei UI", 8.5F);
        _labelSubtitle.ForeColor = Color.FromArgb(139, 92, 246);
        _labelSubtitle.Location = new Point(3, 130);
        _labelSubtitle.MaximumSize = new Size(300, 0);
        _labelSubtitle.Name = "_labelSubtitle";
        _labelSubtitle.Size = new Size(278, 22);
        _labelSubtitle.TabIndex = 4;
        _labelSubtitle.Tag = "score";
        _labelSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // sourceCard
        // 
        sourceCard.BackColor = Color.FromArgb(247, 248, 251);
        sourceCard.Dock = DockStyle.Fill;
        sourceCard.Location = new Point(0, 436);
        sourceCard.Margin = new Padding(0);
        sourceCard.Name = "sourceCard";
        sourceCard.Padding = new Padding(2, 1, 2, 1);
        sourceCard.Size = new Size(308, 188);
        sourceCard.TabIndex = 3;
        sourceCard.Title = "音频来源与匹配";
        // 
        // sourceGrid
        // 
        sourceGrid.BackColor = Color.Transparent;
        sourceGrid.ColumnCount = 2;
        sourceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        sourceGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        sourceGrid.Controls.Add(_btnAddDir, 0, 0);
        sourceGrid.Controls.Add(_btnAiMatch, 1, 0);
        sourceGrid.Controls.Add(_btnRules, 0, 1);
        sourceGrid.Controls.Add(_btnManual, 1, 1);
        sourceGrid.Controls.Add(_btnManualFromList, 0, 2);
        sourceGrid.Controls.Add(_btnRescan, 1, 2);
        sourceGrid.Controls.Add(_btnFollowRead, 0, 3);
        sourceGrid.Controls.Add(_chkSubDir, 1, 3);
        sourceGrid.Dock = DockStyle.Fill;
        sourceGrid.Location = new Point(10, 30);
        sourceGrid.Margin = new Padding(0);
        sourceGrid.Name = "sourceGrid";
        sourceGrid.RowCount = 4;
        sourceGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        sourceGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        sourceGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        sourceGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        sourceGrid.Size = new Size(284, 146);
        sourceGrid.TabIndex = 0;
        // 
        // _btnAddDir
        // 
        _btnAddDir.AutoEllipsis = true;
        _btnAddDir.BackColor = Color.White;
        _btnAddDir.Cursor = Cursors.Hand;
        _btnAddDir.Dock = DockStyle.Fill;
        _btnAddDir.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnAddDir.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnAddDir.FlatStyle = FlatStyle.Flat;
        _btnAddDir.Font = new Font("Microsoft YaHei UI", 9F);
        _btnAddDir.ForeColor = Color.FromArgb(15, 23, 42);
        _btnAddDir.Location = new Point(0, 0);
        _btnAddDir.Margin = new Padding(0, 0, 4, 6);
        _btnAddDir.Name = "_btnAddDir";
        _btnAddDir.Size = new Size(138, 30);
        _btnAddDir.TabIndex = 0;
        _btnAddDir.Tag = "secondary";
        _btnAddDir.Text = "📁 添加音频目录";
        _btnAddDir.UseVisualStyleBackColor = false;
        // 
        // _btnAiMatch
        // 
        _btnAiMatch.AutoEllipsis = true;
        _btnAiMatch.BackColor = Color.White;
        _btnAiMatch.Cursor = Cursors.Hand;
        _btnAiMatch.Dock = DockStyle.Fill;
        _btnAiMatch.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnAiMatch.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnAiMatch.FlatStyle = FlatStyle.Flat;
        _btnAiMatch.Font = new Font("Microsoft YaHei UI", 9F);
        _btnAiMatch.ForeColor = Color.FromArgb(15, 23, 42);
        _btnAiMatch.Location = new Point(146, 0);
        _btnAiMatch.Margin = new Padding(4, 0, 0, 6);
        _btnAiMatch.Name = "_btnAiMatch";
        _btnAiMatch.Size = new Size(138, 30);
        _btnAiMatch.TabIndex = 1;
        _btnAiMatch.Tag = "secondary";
        _btnAiMatch.Text = "✨ AI 匹配";
        _btnAiMatch.UseVisualStyleBackColor = false;
        // 
        // _btnRules
        // 
        _btnRules.AutoEllipsis = true;
        _btnRules.BackColor = Color.White;
        _btnRules.Cursor = Cursors.Hand;
        _btnRules.Dock = DockStyle.Fill;
        _btnRules.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnRules.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnRules.FlatStyle = FlatStyle.Flat;
        _btnRules.Font = new Font("Microsoft YaHei UI", 9F);
        _btnRules.ForeColor = Color.FromArgb(15, 23, 42);
        _btnRules.Location = new Point(0, 36);
        _btnRules.Margin = new Padding(0, 0, 4, 6);
        _btnRules.Name = "_btnRules";
        _btnRules.Size = new Size(138, 30);
        _btnRules.TabIndex = 2;
        _btnRules.Tag = "secondary";
        _btnRules.Text = "规则";
        _btnRules.UseVisualStyleBackColor = false;
        // 
        // _btnManual
        // 
        _btnManual.AutoEllipsis = true;
        _btnManual.BackColor = Color.White;
        _btnManual.Cursor = Cursors.Hand;
        _btnManual.Dock = DockStyle.Fill;
        _btnManual.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnManual.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnManual.FlatStyle = FlatStyle.Flat;
        _btnManual.Font = new Font("Microsoft YaHei UI", 9F);
        _btnManual.ForeColor = Color.FromArgb(15, 23, 42);
        _btnManual.Location = new Point(146, 36);
        _btnManual.Margin = new Padding(4, 0, 0, 6);
        _btnManual.Name = "_btnManual";
        _btnManual.Size = new Size(138, 30);
        _btnManual.TabIndex = 3;
        _btnManual.Tag = "secondary";
        _btnManual.Text = "手动指定";
        _btnManual.UseVisualStyleBackColor = false;
        // 
        // _btnManualFromList
        // 
        _btnManualFromList.AutoEllipsis = true;
        _btnManualFromList.BackColor = Color.White;
        _btnManualFromList.Cursor = Cursors.Hand;
        _btnManualFromList.Dock = DockStyle.Fill;
        _btnManualFromList.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnManualFromList.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnManualFromList.FlatStyle = FlatStyle.Flat;
        _btnManualFromList.Font = new Font("Microsoft YaHei UI", 9F);
        _btnManualFromList.ForeColor = Color.FromArgb(15, 23, 42);
        _btnManualFromList.Location = new Point(0, 72);
        _btnManualFromList.Margin = new Padding(0, 0, 4, 6);
        _btnManualFromList.Name = "_btnManualFromList";
        _btnManualFromList.Size = new Size(138, 30);
        _btnManualFromList.TabIndex = 4;
        _btnManualFromList.Tag = "secondary";
        _btnManualFromList.Text = "从列表选";
        _btnManualFromList.UseVisualStyleBackColor = false;
        // 
        // _btnRescan
        // 
        _btnRescan.AutoEllipsis = true;
        _btnRescan.BackColor = Color.White;
        _btnRescan.Cursor = Cursors.Hand;
        _btnRescan.Dock = DockStyle.Fill;
        _btnRescan.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnRescan.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        _btnRescan.FlatStyle = FlatStyle.Flat;
        _btnRescan.Font = new Font("Microsoft YaHei UI", 9F);
        _btnRescan.ForeColor = Color.FromArgb(15, 23, 42);
        _btnRescan.Location = new Point(146, 72);
        _btnRescan.Margin = new Padding(4, 0, 0, 6);
        _btnRescan.Name = "_btnRescan";
        _btnRescan.Size = new Size(138, 30);
        _btnRescan.TabIndex = 5;
        _btnRescan.Tag = "secondary";
        _btnRescan.Text = "重新扫描";
        _btnRescan.UseVisualStyleBackColor = false;
        // 
        // _btnFollowRead
        // 
        _btnFollowRead.AutoEllipsis = true;
        _btnFollowRead.BackColor = Color.FromArgb(239, 68, 68);
        _btnFollowRead.Cursor = Cursors.Hand;
        _btnFollowRead.Dock = DockStyle.Fill;
        _btnFollowRead.FlatAppearance.BorderSize = 0;
        _btnFollowRead.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
        _btnFollowRead.FlatStyle = FlatStyle.Flat;
        _btnFollowRead.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _btnFollowRead.ForeColor = Color.White;
        _btnFollowRead.Location = new Point(0, 108);
        _btnFollowRead.Margin = new Padding(0, 0, 4, 0);
        _btnFollowRead.Name = "_btnFollowRead";
        _btnFollowRead.Size = new Size(138, 38);
        _btnFollowRead.TabIndex = 6;
        _btnFollowRead.Tag = "danger";
        _btnFollowRead.Text = "● 整页录音";
        _btnFollowRead.UseVisualStyleBackColor = false;
        // 
        // _chkSubDir
        // 
        _chkSubDir.Dock = DockStyle.Fill;
        _chkSubDir.Font = new Font("Microsoft YaHei UI", 9F);
        _chkSubDir.ForeColor = Color.FromArgb(15, 23, 42);
        _chkSubDir.Location = new Point(150, 108);
        _chkSubDir.Margin = new Padding(8, 0, 0, 0);
        _chkSubDir.Name = "_chkSubDir";
        _chkSubDir.Size = new Size(134, 38);
        _chkSubDir.TabIndex = 7;
        _chkSubDir.Text = "包含子目录";
        // 
        // _btnLoop
        // 
        _btnLoop.BackColor = Color.White;
        _btnLoop.Cursor = Cursors.Hand;
        _btnLoop.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnLoop.FlatAppearance.BorderSize = 1;
        _btnLoop.FlatStyle = FlatStyle.Flat;
        _btnLoop.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _btnLoop.ForeColor = Color.FromArgb(15, 23, 42);
        _btnLoop.Location = new Point(129, 0);
        _btnLoop.Margin = new Padding(3, 0, 3, 0);
        _btnLoop.Name = "_btnLoop";
        _btnLoop.Size = new Size(70, 34);
        _btnLoop.TabIndex = 3;
        _btnLoop.Tag = "transport";
        _btnLoop.Text = "🔁 关闭";
        _btnLoop.UseVisualStyleBackColor = false;
        // 
        // PdfAudioPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfAudioPanel";
        Padding = new Padding(6, 2, 6, 6);
        Size = new Size(320, 632);
        root.ResumeLayout(false);
        statusPanel.ResumeLayout(false);
        playback.ResumeLayout(false);
        playback.PerformLayout();
        transport.ResumeLayout(false);
        abRow.ResumeLayout(false);
        speedRow.ResumeLayout(false);
        speedRow.PerformLayout();
        sourceGrid.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private Panel statusPanel;
    private Label _labelInfo;
    private SidebarCard listCard;
    private ListBox _listTracks;
    private SidebarCard playbackCard;
    private TableLayoutPanel playback;
    private FlowLayoutPanel transport;
    private Button _btnPrev;
    private Button _btnPlay;
    private Button _btnNext;
    private Button _btnLoop;
    private TableLayoutPanel abRow;
    private Button _btnSetA;
    private Button _btnSetB;
    private Button _btnClearAb;
    private Label _labelLoop;
    private TableLayoutPanel speedRow;
    private Label speedLabel;
    private ComboBox _comboSpeed;
    private Label speedNote;
    private Label _labelSubtitle;
    private SidebarCard sourceCard;
    private TableLayoutPanel sourceGrid;
    private Button _btnAddDir;
    private Button _btnAiMatch;
    private Button _btnRules;
    private Button _btnManual;
    private Button _btnManualFromList;
    private Button _btnRescan;
    private Button _btnFollowRead;
    private CheckBox _chkSubDir;
    private ToolTip _tooltip;
}
