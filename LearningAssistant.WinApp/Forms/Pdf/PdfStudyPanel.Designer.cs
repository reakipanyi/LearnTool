using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfStudyPanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        header = new SidebarHeaderBar();
        overviewCard = new SidebarCard();
        overviewLayout = new TableLayoutPanel();
        highlightCount = new Label();
        learningCount = new Label();
        sentenceCount = new Label();
        actionCard = new SidebarCard();
        actionLayout = new TableLayoutPanel();
        addHighlightsButton = new Button();
        flashcardsButton = new Button();
        shadowingButton = new Button();
        hint = new Label();
        listCard = new SidebarCard();
        itemList = new FlowLayoutPanel();
        root.SuspendLayout();
        overviewCard.SuspendLayout();
        overviewLayout.SuspendLayout();
        actionCard.SuspendLayout();
        actionLayout.SuspendLayout();
        listCard.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(overviewCard, 0, 1);
        root.Controls.Add(actionCard, 0, 2);
        root.Controls.Add(listCard, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Margin = Padding.Empty;
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 6);
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Size = new Size(288, 700);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.ActionText = "刷新";
        header.Dock = DockStyle.Fill;
        header.Icon = "🎯";
        header.Margin = Padding.Empty;
        header.Name = "header";
        header.Size = new Size(276, 48);
        header.TabIndex = 0;
        header.TitleText = "学习";
        // 
        // overviewCard
        // 
        overviewCard.Body.Controls.Add(overviewLayout);
        overviewCard.Dock = DockStyle.Fill;
        overviewCard.Margin = new Padding(0, 0, 0, 4);
        overviewCard.Name = "overviewCard";
        overviewCard.Size = new Size(276, 146);
        overviewCard.TabIndex = 1;
        overviewCard.Title = "本页概览";
        // 
        // overviewLayout
        // 
        overviewLayout.BackColor = Color.Transparent;
        overviewLayout.ColumnCount = 1;
        overviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        overviewLayout.Controls.Add(highlightCount, 0, 0);
        overviewLayout.Controls.Add(learningCount, 0, 1);
        overviewLayout.Controls.Add(sentenceCount, 0, 2);
        overviewLayout.Dock = DockStyle.Fill;
        overviewLayout.Margin = Padding.Empty;
        overviewLayout.Name = "overviewLayout";
        overviewLayout.Padding = Padding.Empty;
        overviewLayout.RowCount = 3;
        overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.4F));
        overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
        overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
        overviewLayout.Size = new Size(256, 106);
        overviewLayout.TabIndex = 0;
        // 
        // highlightCount
        // 
        highlightCount.AutoEllipsis = true;
        highlightCount.AutoSize = false;
        highlightCount.Dock = DockStyle.Fill;
        highlightCount.Font = new Font("Microsoft YaHei UI", 9.5F);
        highlightCount.ForeColor = Color.FromArgb(15, 23, 42);
        highlightCount.Margin = Padding.Empty;
        highlightCount.Name = "highlightCount";
        highlightCount.Padding = new Padding(2, 0, 0, 0);
        highlightCount.Size = new Size(256, 35);
        highlightCount.TabIndex = 0;
        highlightCount.Text = "—";
        highlightCount.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // learningCount
        // 
        learningCount.AutoEllipsis = true;
        learningCount.AutoSize = false;
        learningCount.Dock = DockStyle.Fill;
        learningCount.Font = new Font("Microsoft YaHei UI", 9.5F);
        learningCount.ForeColor = Color.FromArgb(15, 23, 42);
        learningCount.Margin = Padding.Empty;
        learningCount.Name = "learningCount";
        learningCount.Padding = new Padding(2, 0, 0, 0);
        learningCount.Size = new Size(256, 35);
        learningCount.TabIndex = 1;
        learningCount.Text = "—";
        learningCount.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // sentenceCount
        // 
        sentenceCount.AutoEllipsis = true;
        sentenceCount.AutoSize = false;
        sentenceCount.Dock = DockStyle.Fill;
        sentenceCount.Font = new Font("Microsoft YaHei UI", 9.5F);
        sentenceCount.ForeColor = Color.FromArgb(15, 23, 42);
        sentenceCount.Margin = Padding.Empty;
        sentenceCount.Name = "sentenceCount";
        sentenceCount.Padding = new Padding(2, 0, 0, 0);
        sentenceCount.Size = new Size(256, 35);
        sentenceCount.TabIndex = 2;
        sentenceCount.Text = "—";
        sentenceCount.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // actionCard
        // 
        actionCard.Body.Controls.Add(actionLayout);
        actionCard.Dock = DockStyle.Fill;
        actionCard.Margin = new Padding(0, 0, 0, 4);
        actionCard.Name = "actionCard";
        actionCard.Size = new Size(276, 146);
        actionCard.TabIndex = 2;
        actionCard.Title = "一键操作";
        // 
        // actionLayout
        // 
        actionLayout.BackColor = Color.Transparent;
        actionLayout.ColumnCount = 1;
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionLayout.Controls.Add(addHighlightsButton, 0, 0);
        actionLayout.Controls.Add(flashcardsButton, 0, 1);
        actionLayout.Controls.Add(shadowingButton, 0, 2);
        actionLayout.Controls.Add(hint, 0, 3);
        actionLayout.Dock = DockStyle.Fill;
        actionLayout.Margin = Padding.Empty;
        actionLayout.Name = "actionLayout";
        actionLayout.Padding = Padding.Empty;
        actionLayout.RowCount = 4;
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actionLayout.Size = new Size(256, 106);
        actionLayout.TabIndex = 0;
        // 
        // addHighlightsButton
        // 
        addHighlightsButton.AutoEllipsis = true;
        addHighlightsButton.BackColor = Color.White;
        addHighlightsButton.Cursor = Cursors.Hand;
        addHighlightsButton.Dock = DockStyle.Fill;
        addHighlightsButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        addHighlightsButton.FlatAppearance.BorderSize = 1;
        addHighlightsButton.FlatStyle = FlatStyle.Flat;
        addHighlightsButton.Font = new Font("Microsoft YaHei UI", 9F);
        addHighlightsButton.ForeColor = Color.FromArgb(15, 23, 42);
        addHighlightsButton.Margin = new Padding(0, 0, 0, 6);
        addHighlightsButton.Name = "addHighlightsButton";
        addHighlightsButton.Padding = new Padding(10, 0, 0, 0);
        addHighlightsButton.TabIndex = 0;
        addHighlightsButton.Tag = "secondary";
        addHighlightsButton.Text = "➕ 本页高亮全部加入学习";
        addHighlightsButton.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // flashcardsButton
        // 
        flashcardsButton.AutoEllipsis = true;
        flashcardsButton.BackColor = Color.White;
        flashcardsButton.Cursor = Cursors.Hand;
        flashcardsButton.Dock = DockStyle.Fill;
        flashcardsButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        flashcardsButton.FlatAppearance.BorderSize = 1;
        flashcardsButton.FlatStyle = FlatStyle.Flat;
        flashcardsButton.Font = new Font("Microsoft YaHei UI", 9F);
        flashcardsButton.ForeColor = Color.FromArgb(15, 23, 42);
        flashcardsButton.Margin = new Padding(0, 0, 0, 6);
        flashcardsButton.Name = "flashcardsButton";
        flashcardsButton.Padding = new Padding(10, 0, 0, 0);
        flashcardsButton.TabIndex = 1;
        flashcardsButton.Tag = "secondary";
        flashcardsButton.Text = "✨ 生成本页闪卡";
        flashcardsButton.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // shadowingButton
        // 
        shadowingButton.AutoEllipsis = true;
        shadowingButton.BackColor = Color.White;
        shadowingButton.Cursor = Cursors.Hand;
        shadowingButton.Dock = DockStyle.Fill;
        shadowingButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        shadowingButton.FlatAppearance.BorderSize = 1;
        shadowingButton.FlatStyle = FlatStyle.Flat;
        shadowingButton.Font = new Font("Microsoft YaHei UI", 9F);
        shadowingButton.ForeColor = Color.FromArgb(15, 23, 42);
        shadowingButton.Margin = new Padding(0, 0, 0, 6);
        shadowingButton.Name = "shadowingButton";
        shadowingButton.Padding = new Padding(10, 0, 0, 0);
        shadowingButton.TabIndex = 2;
        shadowingButton.Tag = "secondary";
        shadowingButton.Text = "✂ 开始本页跟读";
        shadowingButton.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // hint
        // 
        hint.AutoSize = false;
        hint.Dock = DockStyle.Fill;
        hint.Font = new Font("Microsoft YaHei UI", 9F);
        hint.ForeColor = Color.FromArgb(100, 116, 139);
        hint.Margin = new Padding(0, 6, 0, 0);
        hint.Name = "hint";
        hint.Size = new Size(256, 30);
        hint.TabIndex = 3;
        hint.Tag = "muted";
        hint.Text = "高亮与闪卡会写入个人学习库，可在学习窗口按来源筛选。";
        hint.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // listCard
        // 
        listCard.Body.Controls.Add(itemList);
        listCard.Dock = DockStyle.Fill;
        listCard.Margin = Padding.Empty;
        listCard.Name = "listCard";
        listCard.Size = new Size(276, 300);
        listCard.TabIndex = 3;
        listCard.Title = "本页学习项";
        // 
        // itemList
        // 
        itemList.AutoScroll = true;
        itemList.BackColor = Color.White;
        itemList.Dock = DockStyle.Fill;
        itemList.FlowDirection = FlowDirection.TopDown;
        itemList.Margin = Padding.Empty;
        itemList.Name = "itemList";
        itemList.Padding = Padding.Empty;
        itemList.Size = new Size(256, 260);
        itemList.TabIndex = 0;
        itemList.WrapContents = false;
        // 
        // PdfStudyPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfStudyPanel";
        Size = new Size(300, 712);
        root.ResumeLayout(false);
        root.PerformLayout();
        overviewCard.ResumeLayout(false);
        overviewCard.PerformLayout();
        overviewLayout.ResumeLayout(false);
        actionCard.ResumeLayout(false);
        actionCard.PerformLayout();
        actionLayout.ResumeLayout(false);
        listCard.ResumeLayout(false);
        listCard.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SidebarHeaderBar header;
    private SidebarCard overviewCard;
    private TableLayoutPanel overviewLayout;
    private Label highlightCount;
    private Label learningCount;
    private Label sentenceCount;
    private SidebarCard actionCard;
    private TableLayoutPanel actionLayout;
    private Button addHighlightsButton;
    private Button flashcardsButton;
    private Button shadowingButton;
    private Label hint;
    private SidebarCard listCard;
    private FlowLayoutPanel itemList;
}
