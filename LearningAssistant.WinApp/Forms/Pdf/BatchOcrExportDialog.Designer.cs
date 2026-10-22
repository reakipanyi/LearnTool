namespace LearningAssistant.Forms.Pdf;

partial class BatchOcrExportDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        accentBar = new Panel();
        labelFiles = new Label();
        labelRange = new Label();
        textRange = new TextBox();
        labelRangeHint = new Label();
        labelOutput = new Label();
        textOutput = new TextBox();
        buttonBrowse = new Button();
        labelFormat = new Label();
        radioMarkdown = new RadioButton();
        radioText = new RadioButton();
        checkAiCleanup = new CheckBox();
        labelAiHint = new Label();
        buttonOk = new Button();
        buttonCancel = new Button();
        SuspendLayout();
        // 
        // accentBar
        // 
        accentBar.BackColor = Color.FromArgb(25, 118, 210);
        accentBar.Dock = DockStyle.Top;
        accentBar.Location = new Point(0, 0);
        accentBar.Name = "accentBar";
        accentBar.Size = new Size(480, 4);
        accentBar.TabIndex = 0;
        // 
        // labelFiles
        // 
        labelFiles.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        labelFiles.ForeColor = Color.FromArgb(50, 50, 60);
        labelFiles.Location = new Point(20, 22);
        labelFiles.Name = "labelFiles";
        labelFiles.Size = new Size(440, 24);
        labelFiles.TabIndex = 1;
        labelFiles.Text = "已选择 1 个 PDF 文件";
        // 
        // labelRange
        // 
        labelRange.ForeColor = Color.FromArgb(50, 50, 60);
        labelRange.Location = new Point(20, 58);
        labelRange.Name = "labelRange";
        labelRange.Size = new Size(300, 22);
        labelRange.TabIndex = 2;
        labelRange.Text = "页码范围（留空表示全部页面）";
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
        labelRangeHint.Size = new Size(230, 22);
        labelRangeHint.TabIndex = 4;
        labelRangeHint.Text = "支持单页或区间，超出页数时按文档实际页数截断";
        // 
        // labelOutput
        // 
        labelOutput.ForeColor = Color.FromArgb(50, 50, 60);
        labelOutput.Location = new Point(20, 120);
        labelOutput.Name = "labelOutput";
        labelOutput.Size = new Size(300, 22);
        labelOutput.TabIndex = 5;
        labelOutput.Text = "输出目录";
        // 
        // textOutput
        // 
        textOutput.Location = new Point(20, 144);
        textOutput.Name = "textOutput";
        textOutput.Size = new Size(356, 27);
        textOutput.TabIndex = 6;
        // 
        // buttonBrowse
        // 
        buttonBrowse.BackColor = Color.White;
        buttonBrowse.Cursor = Cursors.Hand;
        buttonBrowse.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
        buttonBrowse.FlatStyle = FlatStyle.Flat;
        buttonBrowse.Location = new Point(386, 142);
        buttonBrowse.Name = "buttonBrowse";
        buttonBrowse.Size = new Size(74, 30);
        buttonBrowse.TabIndex = 7;
        buttonBrowse.Text = "浏览…";
        buttonBrowse.UseVisualStyleBackColor = false;
        buttonBrowse.Click += ButtonBrowse_Click;
        // 
        // labelFormat
        // 
        labelFormat.ForeColor = Color.FromArgb(50, 50, 60);
        labelFormat.Location = new Point(20, 186);
        labelFormat.Name = "labelFormat";
        labelFormat.Size = new Size(100, 22);
        labelFormat.TabIndex = 8;
        labelFormat.Text = "输出格式";
        // 
        // radioMarkdown
        // 
        radioMarkdown.Checked = true;
        radioMarkdown.ForeColor = Color.FromArgb(50, 50, 60);
        radioMarkdown.Location = new Point(20, 210);
        radioMarkdown.Name = "radioMarkdown";
        radioMarkdown.Size = new Size(160, 24);
        radioMarkdown.TabIndex = 9;
        radioMarkdown.TabStop = true;
        radioMarkdown.Text = "Markdown (.md)";
        radioMarkdown.UseVisualStyleBackColor = true;
        // 
        // radioText
        // 
        radioText.ForeColor = Color.FromArgb(50, 50, 60);
        radioText.Location = new Point(190, 210);
        radioText.Name = "radioText";
        radioText.Size = new Size(140, 24);
        radioText.TabIndex = 10;
        radioText.Text = "纯文本 (.txt)";
        radioText.UseVisualStyleBackColor = true;
        // 
        // checkAiCleanup
        // 
        checkAiCleanup.ForeColor = Color.FromArgb(50, 50, 60);
        checkAiCleanup.Location = new Point(20, 242);
        checkAiCleanup.Name = "checkAiCleanup";
        checkAiCleanup.Size = new Size(440, 24);
        checkAiCleanup.TabIndex = 11;
        checkAiCleanup.Text = "AI 智能整理（标题层级 / 去页眉页脚 / 段落合并 / 术语统一）";
        checkAiCleanup.UseVisualStyleBackColor = true;
        // 
        // labelAiHint
        // 
        labelAiHint.ForeColor = Color.FromArgb(150, 150, 160);
        labelAiHint.Location = new Point(40, 266);
        labelAiHint.Name = "labelAiHint";
        labelAiHint.Size = new Size(420, 22);
        labelAiHint.TabIndex = 12;
        labelAiHint.Text = "整理失败时自动回退原始 OCR 文本，不中断导出";
        // 
        // buttonOk
        // 
        buttonOk.BackColor = Color.FromArgb(25, 118, 210);
        buttonOk.Cursor = Cursors.Hand;
        buttonOk.FlatAppearance.BorderSize = 0;
        buttonOk.FlatStyle = FlatStyle.Flat;
        buttonOk.ForeColor = Color.White;
        buttonOk.Location = new Point(250, 298);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(100, 34);
        buttonOk.TabIndex = 13;
        buttonOk.Text = "开始导出";
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
        buttonCancel.Location = new Point(360, 298);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(100, 34);
        buttonCancel.TabIndex = 14;
        buttonCancel.Text = "取消";
        buttonCancel.UseVisualStyleBackColor = false;
        buttonCancel.Click += ButtonCancel_Click;
        // 
        // BatchOcrExportDialog
        // 
        AcceptButton = buttonOk;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        CancelButton = buttonCancel;
        ClientSize = new Size(480, 350);
        Controls.Add(buttonCancel);
        Controls.Add(buttonOk);
        Controls.Add(labelAiHint);
        Controls.Add(checkAiCleanup);
        Controls.Add(radioText);
        Controls.Add(radioMarkdown);
        Controls.Add(labelFormat);
        Controls.Add(buttonBrowse);
        Controls.Add(textOutput);
        Controls.Add(labelOutput);
        Controls.Add(labelRangeHint);
        Controls.Add(textRange);
        Controls.Add(labelRange);
        Controls.Add(labelFiles);
        Controls.Add(accentBar);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "BatchOcrExportDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "批量 OCR 导出文本";
        ResumeLayout(false);
    }

    #endregion

    private Panel accentBar;
    private Label labelFiles;
    private Label labelRange;
    private TextBox textRange;
    private Label labelRangeHint;
    private Label labelOutput;
    private TextBox textOutput;
    private Button buttonBrowse;
    private Label labelFormat;
    private RadioButton radioMarkdown;
    private RadioButton radioText;
    private CheckBox checkAiCleanup;
    private Label labelAiHint;
    private Button buttonOk;
    private Button buttonCancel;
}
