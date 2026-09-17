namespace LearningAssistant.Forms.Learning;

partial class FolderRenameDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code
    private void InitializeComponent()
    {
        this.lblNameCaption = new System.Windows.Forms.Label();
        this.txtNewName = new System.Windows.Forms.TextBox();
        this.grpSizeOptions = new System.Windows.Forms.GroupBox();
        this.chkAppendSize = new System.Windows.Forms.CheckBox();
        this.pnlSizeOptions = new System.Windows.Forms.Panel();
        this.lblFormatCaption = new System.Windows.Forms.Label();
        this.cboFormat = new System.Windows.Forms.ComboBox();
        this.lblDecimalsCaption = new System.Windows.Forms.Label();
        this.nudDecimals = new System.Windows.Forms.NumericUpDown();
        this.radSuffix = new System.Windows.Forms.RadioButton();
        this.radPrefix = new System.Windows.Forms.RadioButton();
        this.chkShowCount = new System.Windows.Forms.CheckBox();
        this.btnRecalculate = new System.Windows.Forms.Button();
        this.lblSizeInfo = new System.Windows.Forms.Label();
        this.lblPreviewCaption = new System.Windows.Forms.Label();
        this.lblPreview = new System.Windows.Forms.Label();
        this.btnOK = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.grpSizeOptions.SuspendLayout();
        this.pnlSizeOptions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.nudDecimals)).BeginInit();
        this.SuspendLayout();
        //
        // lblNameCaption
        //
        this.lblNameCaption.AutoSize = true;
        this.lblNameCaption.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.lblNameCaption.Location = new System.Drawing.Point(16, 20);
        this.lblNameCaption.Name = "lblNameCaption";
        this.lblNameCaption.Size = new System.Drawing.Size(68, 17);
        this.lblNameCaption.TabIndex = 0;
        this.lblNameCaption.Text = "新名称：";
        //
        // txtNewName
        //
        this.txtNewName.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
        this.txtNewName.Location = new System.Drawing.Point(90, 16);
        this.txtNewName.Name = "txtNewName";
        this.txtNewName.Size = new System.Drawing.Size(380, 25);
        this.txtNewName.TabIndex = 1;
        this.txtNewName.TextChanged += new System.EventHandler(this.txtNewName_TextChanged);
        //
        // grpSizeOptions
        //
        this.grpSizeOptions.Controls.Add(this.chkAppendSize);
        this.grpSizeOptions.Controls.Add(this.pnlSizeOptions);
        this.grpSizeOptions.Controls.Add(this.lblSizeInfo);
        this.grpSizeOptions.Controls.Add(this.btnRecalculate);
        this.grpSizeOptions.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.grpSizeOptions.Location = new System.Drawing.Point(16, 56);
        this.grpSizeOptions.Name = "grpSizeOptions";
        this.grpSizeOptions.Size = new System.Drawing.Size(454, 150);
        this.grpSizeOptions.TabIndex = 2;
        this.grpSizeOptions.TabStop = false;
        this.grpSizeOptions.Text = "名称增强";
        //
        // chkAppendSize
        //
        this.chkAppendSize.AutoSize = true;
        this.chkAppendSize.Checked = false;
        this.chkAppendSize.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.chkAppendSize.Location = new System.Drawing.Point(14, 24);
        this.chkAppendSize.Name = "chkAppendSize";
        this.chkAppendSize.Size = new System.Drawing.Size(174, 21);
        this.chkAppendSize.TabIndex = 0;
        this.chkAppendSize.Text = "追加文件夹总大小后缀";
        this.chkAppendSize.UseVisualStyleBackColor = true;
        this.chkAppendSize.CheckedChanged += new System.EventHandler(this.chkAppendSize_CheckedChanged);
        //
        // pnlSizeOptions (勾选后才启用的详细选项)
        //
        this.pnlSizeOptions.Controls.Add(this.lblFormatCaption);
        this.pnlSizeOptions.Controls.Add(this.cboFormat);
        this.pnlSizeOptions.Controls.Add(this.lblDecimalsCaption);
        this.pnlSizeOptions.Controls.Add(this.nudDecimals);
        this.pnlSizeOptions.Controls.Add(this.radSuffix);
        this.pnlSizeOptions.Controls.Add(this.radPrefix);
        this.pnlSizeOptions.Controls.Add(this.chkShowCount);
        this.pnlSizeOptions.Enabled = false;
        this.pnlSizeOptions.Location = new System.Drawing.Point(14, 48);
        this.pnlSizeOptions.Name = "pnlSizeOptions";
        this.pnlSizeOptions.Size = new System.Drawing.Size(426, 72);
        this.pnlSizeOptions.TabIndex = 1;
        //
        // lblFormatCaption
        //
        this.lblFormatCaption.AutoSize = true;
        this.lblFormatCaption.Location = new System.Drawing.Point(0, 6);
        this.lblFormatCaption.Name = "lblFormatCaption";
        this.lblFormatCaption.Size = new System.Drawing.Size(44, 17);
        this.lblFormatCaption.TabIndex = 0;
        this.lblFormatCaption.Text = "格式：";
        //
        // cboFormat
        //
        this.cboFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboFormat.Items.AddRange(new object[] {
            "_(3.25GB)       圆括号 紧凑",
            "_[3.25 GB]      中括号 ⭐默认",
            "【3.25GB】      中文方括号",
            "-3.25GB         短横线",
            "[3.25 GB]名称   前缀模式"});
        this.cboFormat.Location = new System.Drawing.Point(46, 2);
        this.cboFormat.Name = "cboFormat";
        this.cboFormat.Size = new System.Drawing.Size(180, 25);
        this.cboFormat.TabIndex = 1;
        this.cboFormat.SelectedIndex = 1;
        this.cboFormat.SelectedIndexChanged += new System.EventHandler(this.cboFormat_SelectedIndexChanged);
        //
        // lblDecimalsCaption
        //
        this.lblDecimalsCaption.AutoSize = true;
        this.lblDecimalsCaption.Location = new System.Drawing.Point(236, 6);
        this.lblDecimalsCaption.Name = "lblDecimalsCaption";
        this.lblDecimalsCaption.Size = new System.Drawing.Size(56, 17);
        this.lblDecimalsCaption.TabIndex = 2;
        this.lblDecimalsCaption.Text = "小数位：";
        //
        // nudDecimals
        //
        this.nudDecimals.Location = new System.Drawing.Point(292, 2);
        this.nudDecimals.Maximum = new decimal(new int[] {3, 0, 0, 0});
        this.nudDecimals.Minimum = new decimal(new int[] {0, 0, 0, 0});
        this.nudDecimals.Name = "nudDecimals";
        this.nudDecimals.Size = new System.Drawing.Size(50, 23);
        this.nudDecimals.TabIndex = 3;
        this.nudDecimals.Value = new decimal(new int[] {2, 0, 0, 0});
        this.nudDecimals.ValueChanged += new System.EventHandler(this.nudDecimals_ValueChanged);
        //
        // radSuffix
        //
        this.radSuffix.AutoSize = true;
        this.radSuffix.Checked = true;
        this.radSuffix.Location = new System.Drawing.Point(0, 34);
        this.radSuffix.Name = "radSuffix";
        this.radSuffix.Size = new System.Drawing.Size(62, 21);
        this.radSuffix.TabIndex = 4;
        this.radSuffix.TabStop = true;
        this.radSuffix.Text = "后缀 →";
        this.radSuffix.UseVisualStyleBackColor = true;
        this.radSuffix.CheckedChanged += new System.EventHandler(this.radSuffix_CheckedChanged);
        //
        // radPrefix
        //
        this.radPrefix.AutoSize = true;
        this.radPrefix.Location = new System.Drawing.Point(70, 34);
        this.radPrefix.Name = "radPrefix";
        this.radPrefix.Size = new System.Drawing.Size(62, 21);
        this.radPrefix.TabIndex = 5;
        this.radPrefix.Text = "← 前缀";
        this.radPrefix.UseVisualStyleBackColor = true;
        this.radPrefix.CheckedChanged += new System.EventHandler(this.radPrefix_CheckedChanged);
        //
        // chkShowCount
        //
        this.chkShowCount.AutoSize = true;
        this.chkShowCount.Checked = true;
        this.chkShowCount.Location = new System.Drawing.Point(236, 34);
        this.chkShowCount.Name = "chkShowCount";
        this.chkShowCount.Size = new System.Drawing.Size(188, 21);
        this.chkShowCount.TabIndex = 6;
        this.chkShowCount.Text = "预览显示文件数+子目录数";
        this.chkShowCount.UseVisualStyleBackColor = true;
        this.chkShowCount.CheckedChanged += new System.EventHandler(this.chkShowCount_CheckedChanged);
        //
        // btnRecalculate
        //
        this.btnRecalculate.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.btnRecalculate.Location = new System.Drawing.Point(14, 122);
        this.btnRecalculate.Name = "btnRecalculate";
        this.btnRecalculate.Size = new System.Drawing.Size(130, 22);
        this.btnRecalculate.TabIndex = 2;
        this.btnRecalculate.Text = "🔄 重新计算大小";
        this.btnRecalculate.UseVisualStyleBackColor = true;
        this.btnRecalculate.Click += new System.EventHandler(this.btnRecalculate_Click);
        //
        // lblSizeInfo
        //
        this.lblSizeInfo.AutoSize = false;
        this.lblSizeInfo.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.25F);
        this.lblSizeInfo.Location = new System.Drawing.Point(152, 122);
        this.lblSizeInfo.Name = "lblSizeInfo";
        this.lblSizeInfo.Size = new System.Drawing.Size(288, 22);
        this.lblSizeInfo.TabIndex = 3;
        this.lblSizeInfo.Text = "计算结果：-";
        this.lblSizeInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // lblPreviewCaption
        //
        this.lblPreviewCaption.AutoSize = true;
        this.lblPreviewCaption.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.lblPreviewCaption.Location = new System.Drawing.Point(16, 220);
        this.lblPreviewCaption.Name = "lblPreviewCaption";
        this.lblPreviewCaption.Size = new System.Drawing.Size(80, 17);
        this.lblPreviewCaption.TabIndex = 3;
        this.lblPreviewCaption.Text = "预览最终名称：";
        //
        // lblPreview
        //
        this.lblPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
        this.lblPreview.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(133)))), ((int)(((byte)(244)))));
        this.lblPreview.Location = new System.Drawing.Point(102, 216);
        this.lblPreview.Name = "lblPreview";
        this.lblPreview.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
        this.lblPreview.Size = new System.Drawing.Size(368, 28);
        this.lblPreview.TabIndex = 4;
        this.lblPreview.Text = "（请输入名称）";
        this.lblPreview.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.lblPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        //
        // btnOK
        //
        this.btnOK.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.btnOK.Location = new System.Drawing.Point(264, 260);
        this.btnOK.Name = "btnOK";
        this.btnOK.Size = new System.Drawing.Size(100, 30);
        this.btnOK.TabIndex = 5;
        this.btnOK.Text = "✅ 确认重命名";
        this.btnOK.UseVisualStyleBackColor = true;
        this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
        //
        // btnCancel
        //
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.btnCancel.Location = new System.Drawing.Point(370, 260);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(80, 30);
        this.btnCancel.TabIndex = 6;
        this.btnCancel.Text = "取消";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        //
        // FolderRenameDialog
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.White;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(486, 310);
        this.Controls.Add(this.lblNameCaption);
        this.Controls.Add(this.txtNewName);
        this.Controls.Add(this.grpSizeOptions);
        this.Controls.Add(this.lblPreviewCaption);
        this.Controls.Add(this.lblPreview);
        this.Controls.Add(this.btnOK);
        this.Controls.Add(this.btnCancel);
        this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "FolderRenameDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "📁 文件夹重命名";
        this.grpSizeOptions.ResumeLayout(false);
        this.pnlSizeOptions.ResumeLayout(false);
        this.pnlSizeOptions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.nudDecimals)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
    #endregion

    private System.Windows.Forms.Label lblNameCaption;
    private System.Windows.Forms.TextBox txtNewName;
    private System.Windows.Forms.GroupBox grpSizeOptions;
    private System.Windows.Forms.CheckBox chkAppendSize;
    private System.Windows.Forms.Panel pnlSizeOptions;
    private System.Windows.Forms.Label lblFormatCaption;
    private System.Windows.Forms.ComboBox cboFormat;
    private System.Windows.Forms.Label lblDecimalsCaption;
    private System.Windows.Forms.NumericUpDown nudDecimals;
    private System.Windows.Forms.RadioButton radSuffix;
    private System.Windows.Forms.RadioButton radPrefix;
    private System.Windows.Forms.CheckBox chkShowCount;
    private System.Windows.Forms.Button btnRecalculate;
    private System.Windows.Forms.Label lblSizeInfo;
    private System.Windows.Forms.Label lblPreviewCaption;
    private System.Windows.Forms.Label lblPreview;
    private System.Windows.Forms.Button btnOK;
    private System.Windows.Forms.Button btnCancel;
}