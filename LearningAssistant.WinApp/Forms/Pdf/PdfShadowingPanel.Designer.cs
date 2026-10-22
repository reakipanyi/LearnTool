using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfShadowingPanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        card = new SidebarCard();
        stack = new FlowLayoutPanel();
        description = new Label();
        summaryLabel = new Label();
        openButton = new Button();
        hint = new Label();
        card.SuspendLayout();
        stack.SuspendLayout();
        SuspendLayout();
        // 
        // card
        // 
        card.Body.Controls.Add(stack);
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;
        card.Name = "card";
        card.Size = new Size(300, 440);
        card.TabIndex = 0;
        card.Title = "逐句跟读";
        // 
        // stack
        // 
        stack.BackColor = Color.White;
        stack.Controls.Add(description);
        stack.Controls.Add(summaryLabel);
        stack.Controls.Add(openButton);
        stack.Controls.Add(hint);
        stack.Dock = DockStyle.Fill;
        stack.FlowDirection = FlowDirection.TopDown;
        stack.Margin = Padding.Empty;
        stack.Name = "stack";
        stack.Padding = new Padding(0, 2, 0, 0);
        stack.Size = new Size(280, 400);
        stack.TabIndex = 0;
        stack.WrapContents = false;
        // 
        // description
        // 
        description.AutoSize = false;
        description.Font = new Font("Microsoft YaHei UI", 9F);
        description.ForeColor = Color.FromArgb(100, 116, 139);
        description.Height = 46;
        description.Margin = new Padding(0, 0, 0, 6);
        description.Name = "description";
        description.Size = new Size(250, 46);
        description.TabIndex = 0;
        description.Tag = "muted";
        description.Text = "逐句播放 → 录音 → 查看参考分，可回放原音与自己的录音对比。";
        // 
        // summaryLabel
        // 
        summaryLabel.AutoSize = false;
        summaryLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        summaryLabel.ForeColor = Color.FromArgb(15, 23, 42);
        summaryLabel.Height = 44;
        summaryLabel.Margin = new Padding(0, 0, 0, 10);
        summaryLabel.Name = "summaryLabel";
        summaryLabel.Size = new Size(250, 44);
        summaryLabel.TabIndex = 1;
        summaryLabel.Text = "";
        // 
        // openButton
        // 
        openButton.AutoSize = false;
        openButton.BackColor = Color.FromArgb(108, 92, 231);
        openButton.Cursor = Cursors.Hand;
        openButton.FlatAppearance.BorderSize = 0;
        openButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 58, 185);
        openButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 72, 211);
        openButton.FlatStyle = FlatStyle.Flat;
        openButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        openButton.ForeColor = Color.White;
        openButton.Margin = new Padding(0, 0, 0, 10);
        openButton.Name = "openButton";
        openButton.Size = new Size(150, 34);
        openButton.TabIndex = 2;
        openButton.Tag = "accent";
        openButton.Text = "打开跟读练习";
        // 
        // hint
        // 
        hint.AutoSize = false;
        hint.Font = new Font("Microsoft YaHei UI", 8.5F);
        hint.ForeColor = Color.FromArgb(100, 116, 139);
        hint.Height = 40;
        hint.Margin = Padding.Empty;
        hint.Name = "hint";
        hint.Size = new Size(250, 40);
        hint.TabIndex = 3;
        hint.Tag = "muted";
        hint.Text = "窗口可与 PDF 并排显示，练习时不必关闭原文。";
        // 
        // PdfShadowingPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(card);
        Name = "PdfShadowingPanel";
        Size = new Size(300, 440);
        card.ResumeLayout(false);
        card.PerformLayout();
        stack.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private SidebarCard card;
    private FlowLayoutPanel stack;
    private Label description;
    private Label summaryLabel;
    private Button openButton;
    private Label hint;
}
