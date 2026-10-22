namespace LearningAssistant.Forms.Pdf;

partial class PdfAiNotesExportDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        accentBar = new Panel();
        labelDoc = new Label();
        labelRange = new Label();
        textRange = new TextBox();
        labelRangeHint = new Label();
        labelOutput = new Label();
        textOutput = new TextBox();
        buttonBrowse = new Button();
        labelFormat = new Label();
        checkSaveToNotes = new CheckBox();
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
        accentBar.Size = new Size(480, 4);
        accentBar.TabIndex = 0;
        // 
        // labelDoc
        // 
        labelDoc.AutoEllipsis = true;
        labelDoc.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        labelDoc.ForeColor = Color.FromArgb(50, 50, 60);
        labelDoc.Location = new Point(20, 22);
        labelDoc.Name = "labelDoc";
        labelDoc.Size = new Size(440, 24);
        labelDoc.TabIndex = 1;
        labelDoc.Text = "文档名称（共 1 页）";
        // 
        // labelRange
        // 
        labelRange.ForeColor = Color.FromArgb(50, 50, 60);
        labelRange.Location = new Point(20, 56);
        labelRange.Name = "labelRange";
        labelRange.Size = new Size(300, 22);
        labelRange.TabIndex = 2;
        labelRange.Text = "页码范围（留空表示全部页面）";
        // 
        // textRange
        // 
        textRange.Location = new Point(20, 80);
        textRange.Name = "textRange";
        textRange.PlaceholderText = "例如：1-5";
        textRange.Size = new Size(200, 27);
        textRange.TabIndex = 3;
        // 
        // labelRangeHint
        // 
        labelRangeHint.ForeColor = Color.FromArgb(150, 150, 160);
        labelRangeHint.Location = new Point(230, 84);
        labelRangeHint.Name = "labelRangeHint";
        labelRangeHint.Size = new Size(230, 22);
        labelRangeHint.TabIndex = 4;
        labelRangeHint.Text = "支持单页或区间，超出页数时按文档实际页数截断";
        // 
        // labelOutput
        // 
        labelOutput.ForeColor = Color.FromArgb(50, 50, 60);
        labelOutput.Location = new Point(20, 118);
        labelOutput.Name = "labelOutput";
        labelOutput.Size = new Size(300, 22);
        labelOutput.TabIndex = 5;
        labelOutput.Text = "输出目录";
        // 
        // textOutput
        // 
        textOutput.Location = new Point(20, 142);
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
        buttonBrowse.Location = new Point(386, 140);
        buttonBrowse.Name = "buttonBrowse";
        buttonBrowse.Size = new Size(74, 30);
        buttonBrowse.TabIndex = 7;
        buttonBrowse.Text = "浏览…";
        buttonBrowse.UseVisualStyleBackColor = false;
        buttonBrowse.Click += ButtonBrowse_Click;
        // 
        // labelFormat
        // 
        labelFormat.ForeColor = Color.FromArgb(120, 120, 132);
        labelFormat.Location = new Point(20, 182);
        labelFormat.Name = "labelFormat";
        labelFormat.Size = new Size(440, 22);
        labelFormat.TabIndex = 8;
        labelFormat.Text = "输出格式：Markdown（.md），含页码锚点与结构化标题";
        // 
        // checkSaveToNotes
        // 
        checkSaveToNotes.Checked = true;
        checkSaveToNotes.CheckState = CheckState.Checked;
        checkSaveToNotes.ForeColor = Color.FromArgb(50, 50, 60);
        checkSaveToNotes.Location = new Point(20, 210);
        checkSaveToNotes.Name = "checkSaveToNotes";
        checkSaveToNotes.Size = new Size(440, 24);
        checkSaveToNotes.TabIndex = 9;
        checkSaveToNotes.Text = "同时保存到笔记库，便于后续检索与复习";
        checkSaveToNotes.UseVisualStyleBackColor = true;
        // 
        // buttonOk
        // 
        buttonOk.BackColor = Color.FromArgb(139, 92, 246);
        buttonOk.Cursor = Cursors.Hand;
        buttonOk.FlatAppearance.BorderSize = 0;
        buttonOk.FlatStyle = FlatStyle.Flat;
        buttonOk.ForeColor = Color.White;
        buttonOk.Location = new Point(250, 246);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(100, 34);
        buttonOk.TabIndex = 10;
        buttonOk.Text = "生成并导出";
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
        buttonCancel.Location = new Point(360, 246);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(100, 34);
        buttonCancel.TabIndex = 11;
        buttonCancel.Text = "取消";
        buttonCancel.UseVisualStyleBackColor = false;
        buttonCancel.Click += ButtonCancel_Click;
        // 
        // PdfAiNotesExportDialog
        // 
        AcceptButton = buttonOk;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        CancelButton = buttonCancel;
        ClientSize = new Size(480, 296);
        Controls.Add(buttonCancel);
        Controls.Add(buttonOk);
        Controls.Add(checkSaveToNotes);
        Controls.Add(labelFormat);
        Controls.Add(buttonBrowse);
        Controls.Add(textOutput);
        Controls.Add(labelOutput);
        Controls.Add(labelRangeHint);
        Controls.Add(textRange);
        Controls.Add(labelRange);
        Controls.Add(labelDoc);
        Controls.Add(accentBar);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PdfAiNotesExportDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "AI 学习笔记导出";
        ResumeLayout(false);
    }

    #endregion

    private Panel accentBar;
    private Label labelDoc;
    private Label labelRange;
    private TextBox textRange;
    private Label labelRangeHint;
    private Label labelOutput;
    private TextBox textOutput;
    private Button buttonBrowse;
    private Label labelFormat;
    private CheckBox checkSaveToNotes;
    private Button buttonOk;
    private Button buttonCancel;
}
