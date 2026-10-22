namespace LearningAssistant.Forms.Pdf;

partial class PdfAiFlashcardDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        accentBar = new Panel();
        labelTitle = new Label();
        labelRange = new Label();
        textRange = new TextBox();
        labelRangeHint = new Label();
        labelHint = new Label();
        buttonOk = new Button();
        buttonCancel = new Button();
        SuspendLayout();
        // 
        // accentBar
        // 
        accentBar.BackColor = Color.FromArgb(139, 92, 246);
        accentBar.Dock = DockStyle.Top;
        accentBar.Location = new Point(0, 0);
        accentBar.Name = "accentBar";
        accentBar.Size = new Size(460, 4);
        accentBar.TabIndex = 0;
        // 
        // labelTitle
        // 
        labelTitle.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        labelTitle.ForeColor = Color.FromArgb(50, 50, 60);
        labelTitle.Location = new Point(20, 22);
        labelTitle.Name = "labelTitle";
        labelTitle.Size = new Size(420, 26);
        labelTitle.TabIndex = 1;
        labelTitle.Text = "把文档内容转成问答闪卡";
        // 
        // labelRange
        // 
        labelRange.ForeColor = Color.FromArgb(50, 50, 60);
        labelRange.Location = new Point(20, 58);
        labelRange.Name = "labelRange";
        labelRange.Size = new Size(420, 22);
        labelRange.TabIndex = 2;
        labelRange.Text = "页码范围（共 1 页，留空表示全部页面）";
        // 
        // textRange
        // 
        textRange.Location = new Point(20, 82);
        textRange.Name = "textRange";
        textRange.PlaceholderText = "例如：1-5";
        textRange.Size = new Size(200, 27);
        textRange.TabIndex = 3;
        // 
        // labelRangeHint
        // 
        labelRangeHint.ForeColor = Color.FromArgb(150, 150, 160);
        labelRangeHint.Location = new Point(230, 86);
        labelRangeHint.Name = "labelRangeHint";
        labelRangeHint.Size = new Size(210, 22);
        labelRangeHint.TabIndex = 4;
        labelRangeHint.Text = "支持单页或区间，超出页数时按实际页数截断";
        // 
        // labelHint
        // 
        labelHint.ForeColor = Color.FromArgb(120, 120, 132);
        labelHint.Location = new Point(20, 120);
        labelHint.Name = "labelHint";
        labelHint.Size = new Size(420, 22);
        labelHint.TabIndex = 5;
        labelHint.Text = "生成后可勾选要加入闪卡复习队列的卡片。";
        // 
        // buttonOk
        // 
        buttonOk.BackColor = Color.FromArgb(139, 92, 246);
        buttonOk.Cursor = Cursors.Hand;
        buttonOk.FlatAppearance.BorderSize = 0;
        buttonOk.FlatStyle = FlatStyle.Flat;
        buttonOk.ForeColor = Color.White;
        buttonOk.Location = new Point(236, 164);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(104, 34);
        buttonOk.TabIndex = 6;
        buttonOk.Text = "生成闪卡";
        buttonOk.UseVisualStyleBackColor = false;
        buttonOk.Click += ButtonOk_Click;
        // 
        // buttonCancel
        // 
        buttonCancel.BackColor = Color.White;
        buttonCancel.Cursor = Cursors.Hand;
        buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
        buttonCancel.FlatStyle = FlatStyle.Flat;
        buttonCancel.ForeColor = Color.FromArgb(50, 50, 60);
        buttonCancel.Location = new Point(350, 164);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(90, 34);
        buttonCancel.TabIndex = 7;
        buttonCancel.Text = "取消";
        buttonCancel.UseVisualStyleBackColor = false;
        buttonCancel.Click += ButtonCancel_Click;
        // 
        // PdfAiFlashcardDialog
        // 
        AcceptButton = buttonOk;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        CancelButton = buttonCancel;
        ClientSize = new Size(460, 216);
        Controls.Add(buttonCancel);
        Controls.Add(buttonOk);
        Controls.Add(labelHint);
        Controls.Add(labelRangeHint);
        Controls.Add(textRange);
        Controls.Add(labelRange);
        Controls.Add(labelTitle);
        Controls.Add(accentBar);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PdfAiFlashcardDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "AI 闪卡生成";
        ResumeLayout(false);
    }

    #endregion

    private Panel accentBar;
    private Label labelTitle;
    private Label labelRange;
    private TextBox textRange;
    private Label labelRangeHint;
    private Label labelHint;
    private Button buttonOk;
    private Button buttonCancel;
}
