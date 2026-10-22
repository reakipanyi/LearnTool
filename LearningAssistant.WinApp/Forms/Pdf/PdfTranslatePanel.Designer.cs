using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfTranslatePanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        root = new TableLayoutPanel();
        header = new SidebarHeaderBar();
        body = new TableLayoutPanel();
        originalCard = new SidebarCard();
        translationCard = new SidebarCard();
        actions = new FlowLayoutPanel();
        options = new FlowLayoutPanel();
        root.SuspendLayout();
        body.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(actions, 0, 2);
        root.Controls.Add(options, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 6);
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.Size = new Size(300, 504);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.ActionText = "翻译";
        header.BackColor = Color.FromArgb(247, 248, 251);
        header.Dock = DockStyle.Fill;
        header.Icon = "🌐";
        header.Location = new Point(6, 2);
        header.Margin = new Padding(0);
        header.Name = "header";
        header.Padding = new Padding(2, 0, 2, 0);
        header.Size = new Size(288, 48);
        header.TabIndex = 0;
        header.TitleText = "翻译";
        // 
        // body
        // 
        body.BackColor = Color.FromArgb(247, 248, 251);
        body.ColumnCount = 1;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        body.Controls.Add(originalCard, 0, 0);
        body.Controls.Add(translationCard, 0, 1);
        body.Dock = DockStyle.Fill;
        body.Location = new Point(6, 52);
        body.Margin = new Padding(0, 2, 0, 2);
        body.Name = "body";
        body.RowCount = 2;
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        body.Size = new Size(288, 374);
        body.TabIndex = 1;
        // 
        // originalCard
        // 
        originalCard.ActionText = "🔊 读原文";
        originalCard.BackColor = Color.FromArgb(247, 248, 251);
        originalCard.Dock = DockStyle.Fill;
        originalCard.Location = new Point(0, 0);
        originalCard.Margin = new Padding(0, 0, 0, 2);
        originalCard.Name = "originalCard";
        originalCard.Padding = new Padding(2, 1, 2, 1);
        originalCard.Size = new Size(142, 187);
        originalCard.TabIndex = 0;
        originalCard.Title = "原文";
        // 
        // translationCard
        // 
        translationCard.ActionText = "🔊 读译文";
        translationCard.BackColor = Color.FromArgb(247, 248, 251);
        translationCard.Dock = DockStyle.Fill;
        translationCard.Location = new Point(0, 189);
        translationCard.Margin = new Padding(0, 2, 0, 0);
        translationCard.Name = "translationCard";
        translationCard.Padding = new Padding(2, 1, 2, 1);
        translationCard.Size = new Size(142, 187);
        translationCard.TabIndex = 1;
        translationCard.Title = "译文";
        // 
        // actions
        // 
        actions.BackColor = Color.FromArgb(247, 248, 251);
        actions.Dock = DockStyle.Fill;
        actions.Location = new Point(6, 430);
        actions.Margin = new Padding(0, 2, 0, 0);
        actions.Name = "actions";
        actions.Size = new Size(288, 38);
        actions.TabIndex = 2;
        actions.WrapContents = false;
        // 
        // options
        // 
        options.BackColor = Color.FromArgb(247, 248, 251);
        options.Dock = DockStyle.Fill;
        options.Location = new Point(6, 468);
        options.Margin = new Padding(0);
        options.Name = "options";
        options.Size = new Size(288, 30);
        options.TabIndex = 3;
        options.WrapContents = false;
        // 
        // PdfTranslatePanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfTranslatePanel";
        Size = new Size(300, 504);
        root.ResumeLayout(false);
        body.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SidebarHeaderBar header;
    private TableLayoutPanel body;
    private SidebarCard originalCard;
    private SidebarCard translationCard;
    private FlowLayoutPanel actions;
    private FlowLayoutPanel options;
}
