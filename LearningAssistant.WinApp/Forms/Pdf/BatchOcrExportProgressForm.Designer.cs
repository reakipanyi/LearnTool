namespace LearningAssistant.Forms.Pdf;

partial class BatchOcrExportProgressForm
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        labelStatus = new Label();
        progressBar = new ProgressBar();
        buttonCancel = new Button();
        SuspendLayout();
        // 
        // labelStatus
        // 
        labelStatus.ForeColor = Color.FromArgb(50, 50, 60);
        labelStatus.Location = new Point(20, 20);
        labelStatus.Name = "labelStatus";
        labelStatus.Size = new Size(420, 40);
        labelStatus.TabIndex = 0;
        labelStatus.Text = "准备导出 1 个文件…";
        // 
        // progressBar
        // 
        progressBar.Location = new Point(20, 70);
        progressBar.Maximum = 100;
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(420, 18);
        progressBar.TabIndex = 1;
        // 
        // buttonCancel
        // 
        buttonCancel.BackColor = Color.White;
        buttonCancel.Cursor = Cursors.Hand;
        buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
        buttonCancel.FlatStyle = FlatStyle.Flat;
        buttonCancel.Location = new Point(350, 106);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(90, 30);
        buttonCancel.TabIndex = 2;
        buttonCancel.Text = "取消";
        buttonCancel.UseVisualStyleBackColor = false;
        buttonCancel.Click += ButtonCancel_Click;
        // 
        // BatchOcrExportProgressForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(460, 150);
        Controls.Add(buttonCancel);
        Controls.Add(progressBar);
        Controls.Add(labelStatus);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        Name = "BatchOcrExportProgressForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "批量 OCR 导出";
        ResumeLayout(false);
    }

    #endregion

    private Label labelStatus;
    private ProgressBar progressBar;
    private Button buttonCancel;
}
