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
        bookBar = new Panel();
        _labelBook = new Label();
        _btnBookSwitch = new Button();
        _segments = new SegmentedTabs();
        followRow = new TableLayoutPanel();
        followLabel = new Label();
        pageCard = new PageCardPanel();
        pageLayout = new TableLayoutPanel();
        pageTop = new TableLayoutPanel();
        _labelPageTitle = new Label();
        _labelPageSource = new Label();
        _labelPageTrack = new Label();
        _labelPageMeta = new Label();
        pageActions = new FlowLayoutPanel();
        _btnBind = new Button();
        _btnClearPage = new Button();
        _btnPreview = new Button();
        listCard = new SidebarCard();
        advancedPanel = new TableLayoutPanel();
        advSource = new Panel();
        _labelAdvSource = new Label();
        _labelAdvSourceChevron = new Label();
        advMatch = new Panel();
        _labelAdvMatch = new Label();
        _labelAdvMatchChevron = new Label();
        _listTracks = new ListBox();
        _tooltip = new ToolTip(components);
        root.SuspendLayout();
        statusPanel.SuspendLayout();
        bookBar.SuspendLayout();
        followRow.SuspendLayout();
        pageCard.SuspendLayout();
        pageLayout.SuspendLayout();
        pageTop.SuspendLayout();
        pageActions.SuspendLayout();
        advancedPanel.SuspendLayout();
        advSource.SuspendLayout();
        advMatch.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(statusPanel, 0, 0);
        root.Controls.Add(bookBar, 0, 1);
        root.Controls.Add(_segments, 0, 2);
        root.Controls.Add(followRow, 0, 3);
        root.Controls.Add(pageCard, 0, 4);
        root.Controls.Add(listCard, 0, 5);
        root.Controls.Add(advancedPanel, 0, 6);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(6, 2);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.RowCount = 7;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        root.Size = new Size(308, 632);
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
        statusPanel.Padding = new Padding(10, 5, 10, 3);
        statusPanel.Size = new Size(308, 1);
        statusPanel.TabIndex = 0;
        statusPanel.Tag = "surface";
        statusPanel.Visible = false;
        // 
        // _labelInfo
        // 
        _labelInfo.AutoEllipsis = true;
        _labelInfo.Dock = DockStyle.Fill;
        _labelInfo.Font = new Font("Microsoft YaHei UI", 9F);
        _labelInfo.ForeColor = Color.FromArgb(100, 116, 139);
        _labelInfo.Location = new Point(10, 5);
        _labelInfo.Name = "_labelInfo";
        _labelInfo.Size = new Size(288, 0);
        _labelInfo.TabIndex = 0;
        _labelInfo.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // bookBar
        // 
        bookBar.BackColor = Color.FromArgb(247, 248, 251);
        bookBar.Controls.Add(_labelBook);
        bookBar.Controls.Add(_btnBookSwitch);
        bookBar.Dock = DockStyle.Fill;
        bookBar.Location = new Point(0, 0);
        bookBar.Margin = new Padding(0, 0, 0, 4);
        bookBar.Name = "bookBar";
        bookBar.Size = new Size(308, 34);
        bookBar.TabIndex = 1;
        // 
        // _labelBook
        // 
        _labelBook.AutoEllipsis = true;
        _labelBook.Cursor = Cursors.Hand;
        _labelBook.Dock = DockStyle.Fill;
        _labelBook.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _labelBook.ForeColor = Color.FromArgb(15, 23, 42);
        _labelBook.Location = new Point(0, 0);
        _labelBook.Name = "_labelBook";
        _labelBook.Size = new Size(244, 34);
        _labelBook.TabIndex = 0;
        _labelBook.Text = "未打开文档";
        _labelBook.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _btnBookSwitch
        // 
        _btnBookSwitch.BackColor = Color.White;
        _btnBookSwitch.Cursor = Cursors.Hand;
        _btnBookSwitch.Dock = DockStyle.Right;
        _btnBookSwitch.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnBookSwitch.FlatStyle = FlatStyle.Flat;
        _btnBookSwitch.Font = new Font("Microsoft YaHei UI", 8.5F);
        _btnBookSwitch.ForeColor = Color.FromArgb(71, 85, 105);
        _btnBookSwitch.Location = new Point(244, 0);
        _btnBookSwitch.Margin = new Padding(0, 3, 0, 3);
        _btnBookSwitch.Name = "_btnBookSwitch";
        _btnBookSwitch.Size = new Size(64, 34);
        _btnBookSwitch.TabIndex = 1;
        _btnBookSwitch.Tag = "keep";
        _btnBookSwitch.Text = "切换";
        _btnBookSwitch.UseVisualStyleBackColor = false;
        // 
        // _segments
        // 
        _segments.BackColor = Color.FromArgb(247, 248, 251);
        _segments.Dock = DockStyle.Fill;
        _segments.Location = new Point(0, 38);
        _segments.Margin = new Padding(0, 0, 0, 4);
        _segments.Name = "_segments";
        _segments.Size = new Size(308, 32);
        _segments.TabIndex = 2;
        // 
        // followRow
        // 
        followRow.BackColor = Color.FromArgb(247, 248, 251);
        followRow.ColumnCount = 2;
        followRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        followRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
        followRow.Controls.Add(followLabel, 0, 0);
        followRow.Dock = DockStyle.Fill;
        followRow.Location = new Point(0, 74);
        followRow.Margin = new Padding(0, 0, 0, 4);
        followRow.Name = "followRow";
        followRow.RowCount = 1;
        followRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        followRow.Size = new Size(308, 26);
        followRow.TabIndex = 3;
        // 
        // followLabel
        // 
        followLabel.Dock = DockStyle.Fill;
        followLabel.Font = new Font("Microsoft YaHei UI", 9F);
        followLabel.ForeColor = Color.FromArgb(100, 116, 139);
        followLabel.Location = new Point(0, 0);
        followLabel.Margin = new Padding(0);
        followLabel.Name = "followLabel";
        followLabel.Size = new Size(260, 26);
        followLabel.TabIndex = 0;
        followLabel.Tag = "muted";
        followLabel.Text = "翻页自动跟随";
        followLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pageCard
        // 
        pageCard.BackColor = Color.FromArgb(243, 240, 255);
        pageCard.Controls.Add(pageLayout);
        pageCard.Dock = DockStyle.Fill;
        pageCard.Location = new Point(0, 104);
        pageCard.Margin = new Padding(0, 0, 0, 4);
        pageCard.Name = "pageCard";
        pageCard.Padding = new Padding(10, 7, 10, 7);
        pageCard.Size = new Size(308, 136);
        pageCard.TabIndex = 4;
        // 
        // pageLayout
        // 
        pageLayout.BackColor = Color.Transparent;
        pageLayout.ColumnCount = 1;
        pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pageLayout.Controls.Add(pageTop, 0, 0);
        pageLayout.Controls.Add(_labelPageTrack, 0, 1);
        pageLayout.Controls.Add(_labelPageMeta, 0, 2);
        pageLayout.Controls.Add(pageActions, 0, 3);
        pageLayout.Dock = DockStyle.Fill;
        pageLayout.Location = new Point(10, 7);
        pageLayout.Margin = new Padding(0);
        pageLayout.Name = "pageLayout";
        pageLayout.RowCount = 4;
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pageLayout.Size = new Size(288, 122);
        pageLayout.TabIndex = 0;
        // 
        // pageTop
        // 
        pageTop.BackColor = Color.Transparent;
        pageTop.ColumnCount = 2;
        pageTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pageTop.ColumnStyles.Add(new ColumnStyle());
        pageTop.Controls.Add(_labelPageTitle, 0, 0);
        pageTop.Controls.Add(_labelPageSource, 1, 0);
        pageTop.Dock = DockStyle.Fill;
        pageTop.Location = new Point(0, 0);
        pageTop.Margin = new Padding(0);
        pageTop.Name = "pageTop";
        pageTop.RowCount = 1;
        pageTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pageTop.Size = new Size(288, 18);
        pageTop.TabIndex = 0;
        // 
        // _labelPageTitle
        // 
        _labelPageTitle.AutoEllipsis = true;
        _labelPageTitle.Dock = DockStyle.Fill;
        _labelPageTitle.Font = new Font("Microsoft YaHei UI", 8.5F);
        _labelPageTitle.ForeColor = Color.FromArgb(100, 116, 139);
        _labelPageTitle.Location = new Point(0, 0);
        _labelPageTitle.Margin = new Padding(0);
        _labelPageTitle.Name = "_labelPageTitle";
        _labelPageTitle.Size = new Size(244, 18);
        _labelPageTitle.TabIndex = 0;
        _labelPageTitle.Tag = "muted";
        _labelPageTitle.Text = "当前页 · P1";
        _labelPageTitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _labelPageSource
        // 
        _labelPageSource.AutoSize = true;
        _labelPageSource.Dock = DockStyle.Fill;
        _labelPageSource.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
        _labelPageSource.ForeColor = Color.FromArgb(108, 92, 231);
        _labelPageSource.Location = new Point(244, 0);
        _labelPageSource.Margin = new Padding(0);
        _labelPageSource.Name = "_labelPageSource";
        _labelPageSource.Size = new Size(44, 18);
        _labelPageSource.TabIndex = 1;
        _labelPageSource.Tag = "accent";
        _labelPageSource.Text = "未绑定";
        _labelPageSource.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _labelPageTrack
        // 
        _labelPageTrack.AutoEllipsis = true;
        _labelPageTrack.Dock = DockStyle.Fill;
        _labelPageTrack.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _labelPageTrack.ForeColor = Color.FromArgb(15, 23, 42);
        _labelPageTrack.Location = new Point(0, 18);
        _labelPageTrack.Margin = new Padding(0);
        _labelPageTrack.Name = "_labelPageTrack";
        _labelPageTrack.Size = new Size(288, 24);
        _labelPageTrack.TabIndex = 1;
        _labelPageTrack.Text = "本页未绑定音频";
        _labelPageTrack.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _labelPageMeta
        // 
        _labelPageMeta.AutoEllipsis = true;
        _labelPageMeta.Dock = DockStyle.Fill;
        _labelPageMeta.Font = new Font("Microsoft YaHei UI", 8.5F);
        _labelPageMeta.ForeColor = Color.FromArgb(100, 116, 139);
        _labelPageMeta.Location = new Point(0, 42);
        _labelPageMeta.Margin = new Padding(0);
        _labelPageMeta.Name = "_labelPageMeta";
        _labelPageMeta.Size = new Size(288, 18);
        _labelPageMeta.TabIndex = 2;
        _labelPageMeta.Tag = "muted";
        _labelPageMeta.Text = "点「更换绑定」选择这一页的音频";
        _labelPageMeta.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pageActions
        // 
        pageActions.BackColor = Color.Transparent;
        pageActions.Controls.Add(_btnBind);
        pageActions.Controls.Add(_btnClearPage);
        pageActions.Controls.Add(_btnPreview);
        pageActions.Dock = DockStyle.Fill;
        pageActions.Location = new Point(0, 64);
        pageActions.Margin = new Padding(0, 4, 0, 0);
        pageActions.Name = "pageActions";
        pageActions.Size = new Size(288, 58);
        pageActions.TabIndex = 3;
        pageActions.WrapContents = false;
        // 
        // _btnBind
        // 
        _btnBind.BackColor = Color.FromArgb(108, 92, 231);
        _btnBind.Cursor = Cursors.Hand;
        _btnBind.FlatAppearance.BorderSize = 0;
        _btnBind.FlatStyle = FlatStyle.Flat;
        _btnBind.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _btnBind.ForeColor = Color.White;
        _btnBind.Location = new Point(0, 0);
        _btnBind.Margin = new Padding(0, 0, 6, 0);
        _btnBind.Name = "_btnBind";
        _btnBind.Size = new Size(86, 30);
        _btnBind.TabIndex = 0;
        _btnBind.Tag = "accent";
        _btnBind.Text = "更换绑定";
        _btnBind.UseVisualStyleBackColor = false;
        // 
        // _btnClearPage
        // 
        _btnClearPage.BackColor = Color.White;
        _btnClearPage.Cursor = Cursors.Hand;
        _btnClearPage.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnClearPage.FlatStyle = FlatStyle.Flat;
        _btnClearPage.Font = new Font("Microsoft YaHei UI", 9F);
        _btnClearPage.ForeColor = Color.FromArgb(71, 85, 105);
        _btnClearPage.Location = new Point(92, 0);
        _btnClearPage.Margin = new Padding(0, 0, 6, 0);
        _btnClearPage.Name = "_btnClearPage";
        _btnClearPage.Size = new Size(76, 30);
        _btnClearPage.TabIndex = 1;
        _btnClearPage.Tag = "keep";
        _btnClearPage.Text = "清除本页";
        _btnClearPage.UseVisualStyleBackColor = false;
        // 
        // _btnPreview
        // 
        _btnPreview.BackColor = Color.White;
        _btnPreview.Cursor = Cursors.Hand;
        _btnPreview.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        _btnPreview.FlatStyle = FlatStyle.Flat;
        _btnPreview.Font = new Font("Microsoft YaHei UI", 9F);
        _btnPreview.ForeColor = Color.FromArgb(71, 85, 105);
        _btnPreview.Location = new Point(174, 0);
        _btnPreview.Margin = new Padding(0);
        _btnPreview.Name = "_btnPreview";
        _btnPreview.Size = new Size(54, 30);
        _btnPreview.TabIndex = 2;
        _btnPreview.Tag = "keep";
        _btnPreview.Text = "试听";
        _btnPreview.UseVisualStyleBackColor = false;
        // 
        // listCard
        // 
        listCard.BackColor = Color.FromArgb(247, 248, 251);
        listCard.Dock = DockStyle.Fill;
        listCard.Location = new Point(0, 244);
        listCard.Margin = new Padding(0, 0, 0, 4);
        listCard.Name = "listCard";
        listCard.Padding = new Padding(2, 1, 2, 1);
        listCard.Size = new Size(308, 312);
        listCard.TabIndex = 5;
        listCard.Title = "曲目";
        // 
        // advancedPanel
        // 
        advancedPanel.BackColor = Color.FromArgb(247, 248, 251);
        advancedPanel.ColumnCount = 1;
        advancedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        advancedPanel.Controls.Add(advSource, 0, 0);
        advancedPanel.Controls.Add(advMatch, 0, 1);
        advancedPanel.Dock = DockStyle.Fill;
        advancedPanel.Location = new Point(0, 560);
        advancedPanel.Margin = new Padding(0);
        advancedPanel.Name = "advancedPanel";
        advancedPanel.RowCount = 2;
        advancedPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        advancedPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        advancedPanel.Size = new Size(308, 72);
        advancedPanel.TabIndex = 6;
        // 
        // advSource
        // 
        advSource.BackColor = Color.FromArgb(247, 248, 251);
        advSource.Controls.Add(_labelAdvSource);
        advSource.Controls.Add(_labelAdvSourceChevron);
        advSource.Cursor = Cursors.Hand;
        advSource.Dock = DockStyle.Fill;
        advSource.Location = new Point(0, 0);
        advSource.Margin = new Padding(0);
        advSource.Name = "advSource";
        advSource.Size = new Size(308, 34);
        advSource.TabIndex = 0;
        // 
        // _labelAdvSource
        // 
        _labelAdvSource.Dock = DockStyle.Fill;
        _labelAdvSource.Font = new Font("Microsoft YaHei UI", 9F);
        _labelAdvSource.ForeColor = Color.FromArgb(100, 116, 139);
        _labelAdvSource.Location = new Point(0, 0);
        _labelAdvSource.Margin = new Padding(0);
        _labelAdvSource.Name = "_labelAdvSource";
        _labelAdvSource.Size = new Size(286, 34);
        _labelAdvSource.TabIndex = 0;
        _labelAdvSource.Tag = "muted";
        _labelAdvSource.Text = "音频来源与扫描";
        _labelAdvSource.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _labelAdvSourceChevron
        // 
        _labelAdvSourceChevron.Dock = DockStyle.Right;
        _labelAdvSourceChevron.Font = new Font("Microsoft YaHei UI", 10F);
        _labelAdvSourceChevron.ForeColor = Color.FromArgb(148, 163, 184);
        _labelAdvSourceChevron.Location = new Point(286, 0);
        _labelAdvSourceChevron.Margin = new Padding(0);
        _labelAdvSourceChevron.Name = "_labelAdvSourceChevron";
        _labelAdvSourceChevron.Size = new Size(22, 34);
        _labelAdvSourceChevron.TabIndex = 1;
        _labelAdvSourceChevron.Tag = "muted";
        _labelAdvSourceChevron.Text = "›";
        _labelAdvSourceChevron.TextAlign = ContentAlignment.MiddleRight;
        // 
        // advMatch
        // 
        advMatch.BackColor = Color.FromArgb(247, 248, 251);
        advMatch.Controls.Add(_labelAdvMatch);
        advMatch.Controls.Add(_labelAdvMatchChevron);
        advMatch.Cursor = Cursors.Hand;
        advMatch.Dock = DockStyle.Fill;
        advMatch.Location = new Point(0, 34);
        advMatch.Margin = new Padding(0);
        advMatch.Name = "advMatch";
        advMatch.Size = new Size(308, 38);
        advMatch.TabIndex = 1;
        // 
        // _labelAdvMatch
        // 
        _labelAdvMatch.Dock = DockStyle.Fill;
        _labelAdvMatch.Font = new Font("Microsoft YaHei UI", 9F);
        _labelAdvMatch.ForeColor = Color.FromArgb(100, 116, 139);
        _labelAdvMatch.Location = new Point(0, 0);
        _labelAdvMatch.Margin = new Padding(0);
        _labelAdvMatch.Name = "_labelAdvMatch";
        _labelAdvMatch.Size = new Size(286, 38);
        _labelAdvMatch.TabIndex = 0;
        _labelAdvMatch.Tag = "muted";
        _labelAdvMatch.Text = "高级匹配 · AI / 规则";
        _labelAdvMatch.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _labelAdvMatchChevron
        // 
        _labelAdvMatchChevron.Dock = DockStyle.Right;
        _labelAdvMatchChevron.Font = new Font("Microsoft YaHei UI", 10F);
        _labelAdvMatchChevron.ForeColor = Color.FromArgb(148, 163, 184);
        _labelAdvMatchChevron.Location = new Point(286, 0);
        _labelAdvMatchChevron.Margin = new Padding(0);
        _labelAdvMatchChevron.Name = "_labelAdvMatchChevron";
        _labelAdvMatchChevron.Size = new Size(22, 38);
        _labelAdvMatchChevron.TabIndex = 1;
        _labelAdvMatchChevron.Tag = "muted";
        _labelAdvMatchChevron.Text = "›";
        _labelAdvMatchChevron.TextAlign = ContentAlignment.MiddleRight;
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
        _listTracks.Size = new Size(284, 256);
        _listTracks.TabIndex = 0;
        // 
        // PdfAudioPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfAudioPanel";
        Padding = new Padding(6, 2, 6, 6);
        Size = new Size(320, 640);
        root.ResumeLayout(false);
        statusPanel.ResumeLayout(false);
        bookBar.ResumeLayout(false);
        followRow.ResumeLayout(false);
        pageCard.ResumeLayout(false);
        pageLayout.ResumeLayout(false);
        pageTop.ResumeLayout(false);
        pageTop.PerformLayout();
        pageActions.ResumeLayout(false);
        advancedPanel.ResumeLayout(false);
        advSource.ResumeLayout(false);
        advMatch.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private Panel statusPanel;
    private Label _labelInfo;
    private Panel bookBar;
    private Label _labelBook;
    private Button _btnBookSwitch;
    private SegmentedTabs _segments;
    private TableLayoutPanel followRow;
    private Label followLabel;
    private PageCardPanel pageCard;
    private TableLayoutPanel pageLayout;
    private TableLayoutPanel pageTop;
    private Label _labelPageTitle;
    private Label _labelPageSource;
    private Label _labelPageTrack;
    private Label _labelPageMeta;
    private FlowLayoutPanel pageActions;
    private Button _btnBind;
    private Button _btnClearPage;
    private Button _btnPreview;
    private SidebarCard listCard;
    private ListBox _listTracks;
    private TableLayoutPanel advancedPanel;
    private Panel advSource;
    private Label _labelAdvSource;
    private Label _labelAdvSourceChevron;
    private Panel advMatch;
    private Label _labelAdvMatch;
    private Label _labelAdvMatchChevron;
    private ToolTip _tooltip;
}
