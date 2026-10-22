using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfTranslatePanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
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
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(actions, 0, 2);
        root.Controls.Add(options, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Margin = Padding.Empty;
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 6);
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.Size = new Size(288, 492);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.ActionText = "翻译";
        header.Dock = DockStyle.Fill;
        header.Icon = "🌐";
        header.Margin = Padding.Empty;
        header.Name = "header";
        header.Size = new Size(276, 48);
        header.TabIndex = 0;
        header.TitleText = "翻译";
        // 
        // body
        // 
        body.BackColor = Color.FromArgb(247, 248, 251);
        body.ColumnCount = 2;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        body.Controls.Add(originalCard, 0, 0);
        body.Controls.Add(translationCard, 1, 0);
        body.Dock = DockStyle.Fill;
        body.Margin = new Padding(0, 2, 0, 2);
        body.Name = "body";
        body.RowCount = 1;
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.Size = new Size(276, 406);
        body.TabIndex = 1;
        // 
        // originalCard
        // 
        originalCard.ActionText = "🔊 读原文";
        originalCard.Dock = DockStyle.Fill;
        originalCard.Margin = new Padding(0, 0, 2, 0);
        originalCard.Name = "originalCard";
        originalCard.Size = new Size(136, 406);
        originalCard.TabIndex = 0;
        originalCard.Title = "原文";
        // 
        // translationCard
        // 
        translationCard.ActionText = "🔊 读译文";
        translationCard.Dock = DockStyle.Fill;
        translationCard.Margin = new Padding(2, 0, 0, 0);
        translationCard.Name = "translationCard";
        translationCard.Size = new Size(138, 406);
        translationCard.TabIndex = 1;
        translationCard.Title = "译文";
        // 
        // actions
        // 
        actions.BackColor = Color.FromArgb(247, 248, 251);
        actions.Dock = DockStyle.Fill;
        actions.FlowDirection = FlowDirection.LeftToRight;
        actions.Margin = new Padding(0, 2, 0, 0);
        actions.Name = "actions";
        actions.Size = new Size(276, 36);
        actions.TabIndex = 2;
        actions.WrapContents = false;
        // 
        // options
        // 
        options.BackColor = Color.FromArgb(247, 248, 251);
        options.Dock = DockStyle.Fill;
        options.FlowDirection = FlowDirection.LeftToRight;
        options.Margin = Padding.Empty;
        options.Name = "options";
        options.Size = new Size(276, 30);
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
        root.PerformLayout();
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
