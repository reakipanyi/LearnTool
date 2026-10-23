namespace LearningAssistant.Forms.Pdf;

partial class PdfAudioRuleDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        topBar = new FlowLayoutPanel();
        labelMatchMode = new Label();
        cmbMatchMode = new ComboBox();
        labelSingleAudio = new Label();
        btnPickSingle = new Button();
        splitContainer = new SplitContainer();
        lvRules = new ListView();
        columnName = new ColumnHeader();
        columnMode = new ColumnHeader();
        columnPriority = new ColumnHeader();
        columnEnabled = new ColumnHeader();
        columnPattern = new ColumnHeader();
        lstPreview = new ListBox();
        labelDiagnostics = new Label();
        labelPreviewSummary = new Label();
        editPanel = new TableLayoutPanel();
        labelName = new Label();
        txtName = new TextBox();
        labelPattern = new Label();
        txtPattern = new TextBox();
        labelSyntax = new Label();
        cmbSyntax = new ComboBox();
        labelModeHint = new Label();
        labelMode = new Label();
        cmbMode = new ComboBox();
        labelCapture = new Label();
        txtCapture = new TextBox();
        labelOffset = new Label();
        numOffset = new NumericUpDown();
        labelPagesPerUnit = new Label();
        numPagesPerUnit = new NumericUpDown();
        labelPriority = new Label();
        numPriority = new NumericUpDown();
        labelEnabled = new Label();
        chkEnabled = new CheckBox();
        bottom = new FlowLayoutPanel();
        btnAdd = new Button();
        btnDelete = new Button();
        btnUp = new Button();
        btnDown = new Button();
        btnApply = new Button();
        btnSuggest = new Button();
        btnClearRules = new Button();
        btnReset = new Button();
        btnOk = new Button();
        btnCancel = new Button();
        tooltip = new ToolTip(components);
        previewTimer = new System.Windows.Forms.Timer(components);
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        editPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numOffset).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numPagesPerUnit).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numPriority).BeginInit();
        bottom.SuspendLayout();
        topBar.SuspendLayout();
        SuspendLayout();
        // 
        // topBar
        // 
        topBar.Dock = DockStyle.Top;
        topBar.Location = new Point(0, 0);
        topBar.Name = "topBar";
        topBar.Padding = new Padding(8, 8, 8, 4);
        topBar.Size = new Size(1000, 44);
        topBar.TabIndex = 3;
        topBar.WrapContents = false;
        topBar.Controls.Add(labelMatchMode);
        topBar.Controls.Add(cmbMatchMode);
        topBar.Controls.Add(labelSingleAudio);
        topBar.Controls.Add(btnPickSingle);
        // 
        // labelMatchMode
        // 
        labelMatchMode.AutoSize = true;
        labelMatchMode.Margin = new Padding(0, 6, 4, 0);
        labelMatchMode.Name = "labelMatchMode";
        labelMatchMode.Size = new Size(55, 17);
        labelMatchMode.TabIndex = 0;
        labelMatchMode.Text = "匹配方式";
        // 
        // cmbMatchMode
        // 
        cmbMatchMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbMatchMode.Margin = new Padding(0, 2, 12, 0);
        cmbMatchMode.Name = "cmbMatchMode";
        cmbMatchMode.Size = new Size(240, 25);
        cmbMatchMode.TabIndex = 1;
        cmbMatchMode.SelectedIndexChanged += CmbMatchMode_SelectedIndexChanged;
        // 
        // labelSingleAudio
        // 
        labelSingleAudio.AutoEllipsis = true;
        labelSingleAudio.Margin = new Padding(0, 4, 4, 0);
        labelSingleAudio.Name = "labelSingleAudio";
        labelSingleAudio.Size = new Size(360, 24);
        labelSingleAudio.TabIndex = 2;
        labelSingleAudio.Text = "未选择音频";
        labelSingleAudio.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnPickSingle
        // 
        btnPickSingle.Margin = new Padding(0, 2, 0, 0);
        btnPickSingle.Name = "btnPickSingle";
        btnPickSingle.Size = new Size(100, 26);
        btnPickSingle.TabIndex = 3;
        btnPickSingle.Text = "选择音频…";
        btnPickSingle.UseVisualStyleBackColor = true;
        btnPickSingle.Click += ButtonPickSingle_Click;
        // 
        // splitContainer
        // 
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.FixedPanel = FixedPanel.Panel1;
        splitContainer.Location = new Point(0, 0);
        splitContainer.Name = "splitContainer";
        splitContainer.Orientation = Orientation.Horizontal;
        // 
        // splitContainer.Panel1
        // 
        splitContainer.Panel1.Controls.Add(lvRules);
        // 
        // splitContainer.Panel2
        // 
        splitContainer.Panel2.Controls.Add(lstPreview);
        splitContainer.Panel2.Controls.Add(labelDiagnostics);
        splitContainer.Panel2.Controls.Add(labelPreviewSummary);
        splitContainer.Size = new Size(489, 511);
        splitContainer.SplitterDistance = 260;
        splitContainer.TabIndex = 0;
        // 
        // lvRules
        // 
        lvRules.Columns.AddRange(new ColumnHeader[] { columnName, columnMode, columnPriority, columnEnabled, columnPattern });
        lvRules.Dock = DockStyle.Fill;
        lvRules.FullRowSelect = true;
        lvRules.GridLines = true;
        lvRules.Location = new Point(0, 0);
        lvRules.Name = "lvRules";
        lvRules.Size = new Size(489, 260);
        lvRules.TabIndex = 0;
        lvRules.UseCompatibleStateImageBehavior = false;
        lvRules.View = View.Details;
        lvRules.SelectedIndexChanged += LvRules_SelectedIndexChanged;
        // 
        // columnName
        // 
        columnName.Text = "名称";
        columnName.Width = 140;
        // 
        // columnMode
        // 
        columnMode.Text = "模式";
        columnMode.Width = 90;
        // 
        // columnPriority
        // 
        columnPriority.Text = "优先级";
        columnPriority.Width = 60;
        // 
        // columnEnabled
        // 
        columnEnabled.Text = "启用";
        columnEnabled.Width = 50;
        // 
        // columnPattern
        // 
        columnPattern.Text = "匹配内容";
        columnPattern.Width = 360;
        // 
        // lstPreview
        // 
        lstPreview.Dock = DockStyle.Fill;
        lstPreview.IntegralHeight = false;
        lstPreview.ItemHeight = 17;
        lstPreview.Location = new Point(0, 44);
        lstPreview.Name = "lstPreview";
        lstPreview.Size = new Size(489, 203);
        lstPreview.TabIndex = 2;
        // 
        // labelDiagnostics
        // 
        labelDiagnostics.Dock = DockStyle.Top;
        labelDiagnostics.ForeColor = Color.FromArgb(220, 38, 38);
        labelDiagnostics.Location = new Point(0, 22);
        labelDiagnostics.Name = "labelDiagnostics";
        labelDiagnostics.Size = new Size(489, 22);
        labelDiagnostics.TabIndex = 1;
        // 
        // labelPreviewSummary
        // 
        labelPreviewSummary.Dock = DockStyle.Top;
        labelPreviewSummary.ForeColor = Color.FromArgb(50, 70, 90);
        labelPreviewSummary.Location = new Point(0, 0);
        labelPreviewSummary.Name = "labelPreviewSummary";
        labelPreviewSummary.Size = new Size(489, 22);
        labelPreviewSummary.TabIndex = 0;
        labelPreviewSummary.Text = "试算：请先打开 PDF";
        // 
        // editPanel
        // 
        editPanel.ColumnCount = 2;
        editPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editPanel.Controls.Add(labelName, 0, 0);
        editPanel.Controls.Add(txtName, 1, 0);
        editPanel.Controls.Add(labelPattern, 0, 1);
        editPanel.Controls.Add(txtPattern, 1, 1);
        editPanel.Controls.Add(labelSyntax, 0, 2);
        editPanel.Controls.Add(cmbSyntax, 1, 2);
        editPanel.Controls.Add(labelModeHint, 0, 3);
        editPanel.Controls.Add(labelMode, 0, 4);
        editPanel.Controls.Add(cmbMode, 1, 4);
        editPanel.Controls.Add(labelCapture, 0, 5);
        editPanel.Controls.Add(txtCapture, 1, 5);
        editPanel.Controls.Add(labelOffset, 0, 6);
        editPanel.Controls.Add(numOffset, 1, 6);
        editPanel.Controls.Add(labelPagesPerUnit, 0, 7);
        editPanel.Controls.Add(numPagesPerUnit, 1, 7);
        editPanel.Controls.Add(labelPriority, 0, 8);
        editPanel.Controls.Add(numPriority, 1, 8);
        editPanel.Controls.Add(labelEnabled, 0, 9);
        editPanel.Controls.Add(chkEnabled, 1, 9);
        editPanel.Dock = DockStyle.Right;
        editPanel.Location = new Point(489, 0);
        editPanel.Name = "editPanel";
        editPanel.Padding = new Padding(8);
        editPanel.RowCount = 10;
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editPanel.Size = new Size(331, 511);
        editPanel.TabIndex = 1;
        editPanel.SetColumnSpan(labelModeHint, 2);
        // 
        // labelName
        // 
        labelName.Anchor = AnchorStyles.Left;
        labelName.AutoSize = true;
        labelName.Location = new Point(8, 14);
        labelName.Margin = new Padding(0, 6, 8, 6);
        labelName.Name = "labelName";
        labelName.Size = new Size(31, 17);
        labelName.TabIndex = 0;
        labelName.Text = "名称";
        // 
        // txtName
        // 
        txtName.Dock = DockStyle.Top;
        txtName.Location = new Point(47, 12);
        txtName.Margin = new Padding(0, 4, 0, 4);
        txtName.Name = "txtName";
        txtName.Size = new Size(276, 27);
        txtName.TabIndex = 1;
        txtName.TextChanged += Field_TextChanged;
        // 
        // labelPattern
        // 
        labelPattern.Anchor = AnchorStyles.Left;
        labelPattern.AutoSize = true;
        labelPattern.Location = new Point(8, 51);
        labelPattern.Margin = new Padding(0, 6, 8, 6);
        labelPattern.Name = "labelPattern";
        labelPattern.Size = new Size(55, 17);
        labelPattern.TabIndex = 2;
        labelPattern.Text = "匹配内容";
        // 
        // txtPattern
        // 
        txtPattern.Dock = DockStyle.Top;
        txtPattern.Location = new Point(47, 49);
        txtPattern.Margin = new Padding(0, 4, 0, 4);
        txtPattern.Name = "txtPattern";
        txtPattern.Size = new Size(276, 27);
        txtPattern.TabIndex = 3;
        txtPattern.TextChanged += TxtPattern_TextChanged;
        // 
        // labelSyntax
        // 
        labelSyntax.Anchor = AnchorStyles.Left;
        labelSyntax.AutoSize = true;
        labelSyntax.Location = new Point(8, 88);
        labelSyntax.Margin = new Padding(0, 6, 8, 6);
        labelSyntax.Name = "labelSyntax";
        labelSyntax.Size = new Size(31, 17);
        labelSyntax.TabIndex = 4;
        labelSyntax.Text = "写法";
        // 
        // cmbSyntax
        // 
        cmbSyntax.Dock = DockStyle.Top;
        cmbSyntax.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbSyntax.Location = new Point(47, 86);
        cmbSyntax.Margin = new Padding(0, 4, 0, 4);
        cmbSyntax.Name = "cmbSyntax";
        cmbSyntax.Size = new Size(276, 25);
        cmbSyntax.TabIndex = 5;
        cmbSyntax.SelectedIndexChanged += CmbSyntax_SelectedIndexChanged;
        // 
        // labelModeHint
        // 
        labelModeHint.AutoSize = true;
        labelModeHint.ForeColor = Color.FromArgb(100, 116, 139);
        labelModeHint.Location = new Point(8, 123);
        labelModeHint.Margin = new Padding(0, 0, 0, 6);
        labelModeHint.MaximumSize = new Size(300, 0);
        labelModeHint.Name = "labelModeHint";
        labelModeHint.Size = new Size(300, 17);
        labelModeHint.TabIndex = 6;
        labelModeHint.Text = "页码 = 捕获数字 + 偏移，例如 P12.mp3 配 -1 映射到第 11 页";
        // 
        // labelMode
        // 
        labelMode.Anchor = AnchorStyles.Left;
        labelMode.AutoSize = true;
        labelMode.Location = new Point(8, 152);
        labelMode.Margin = new Padding(0, 6, 8, 6);
        labelMode.Name = "labelMode";
        labelMode.Size = new Size(31, 17);
        labelMode.TabIndex = 7;
        labelMode.Text = "模式";
        // 
        // cmbMode
        // 
        cmbMode.Dock = DockStyle.Top;
        cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbMode.Location = new Point(47, 150);
        cmbMode.Margin = new Padding(0, 4, 0, 4);
        cmbMode.Name = "cmbMode";
        cmbMode.Size = new Size(276, 25);
        cmbMode.TabIndex = 8;
        cmbMode.SelectedIndexChanged += CmbMode_SelectedIndexChanged;
        // 
        // labelCapture
        // 
        labelCapture.Anchor = AnchorStyles.Left;
        labelCapture.AutoSize = true;
        labelCapture.Location = new Point(8, 189);
        labelCapture.Margin = new Padding(0, 6, 8, 6);
        labelCapture.Name = "labelCapture";
        labelCapture.Size = new Size(78, 17);
        labelCapture.TabIndex = 9;
        labelCapture.Text = "捕获组(1起)";
        // 
        // txtCapture
        // 
        txtCapture.Dock = DockStyle.Top;
        txtCapture.Location = new Point(94, 187);
        txtCapture.Margin = new Padding(0, 4, 0, 4);
        txtCapture.Name = "txtCapture";
        txtCapture.Size = new Size(229, 27);
        txtCapture.TabIndex = 10;
        txtCapture.TextChanged += Field_TextChanged;
        // 
        // labelOffset
        // 
        labelOffset.Anchor = AnchorStyles.Left;
        labelOffset.AutoSize = true;
        labelOffset.Location = new Point(8, 226);
        labelOffset.Margin = new Padding(0, 6, 8, 6);
        labelOffset.Name = "labelOffset";
        labelOffset.Size = new Size(55, 17);
        labelOffset.TabIndex = 11;
        labelOffset.Text = "页码偏移";
        // 
        // numOffset
        // 
        numOffset.Dock = DockStyle.Top;
        numOffset.Location = new Point(47, 224);
        numOffset.Margin = new Padding(0, 4, 0, 4);
        numOffset.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        numOffset.Minimum = new decimal(new int[] { 1000, 0, 0, -2147483648 });
        numOffset.Name = "numOffset";
        numOffset.Size = new Size(276, 27);
        numOffset.TabIndex = 12;
        numOffset.ValueChanged += Field_ValueChanged;
        // 
        // labelPagesPerUnit
        // 
        labelPagesPerUnit.Anchor = AnchorStyles.Left;
        labelPagesPerUnit.AutoSize = true;
        labelPagesPerUnit.Location = new Point(8, 263);
        labelPagesPerUnit.Margin = new Padding(0, 6, 8, 6);
        labelPagesPerUnit.Name = "labelPagesPerUnit";
        labelPagesPerUnit.Size = new Size(62, 17);
        labelPagesPerUnit.TabIndex = 13;
        labelPagesPerUnit.Text = "每单元页数";
        // 
        // numPagesPerUnit
        // 
        numPagesPerUnit.Dock = DockStyle.Top;
        numPagesPerUnit.Location = new Point(47, 261);
        numPagesPerUnit.Margin = new Padding(0, 4, 0, 4);
        numPagesPerUnit.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        numPagesPerUnit.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numPagesPerUnit.Name = "numPagesPerUnit";
        numPagesPerUnit.Size = new Size(276, 27);
        numPagesPerUnit.TabIndex = 14;
        numPagesPerUnit.Value = new decimal(new int[] { 1, 0, 0, 0 });
        numPagesPerUnit.ValueChanged += Field_ValueChanged;
        // 
        // labelPriority
        // 
        labelPriority.Anchor = AnchorStyles.Left;
        labelPriority.AutoSize = true;
        labelPriority.Location = new Point(8, 300);
        labelPriority.Margin = new Padding(0, 6, 8, 6);
        labelPriority.Name = "labelPriority";
        labelPriority.Size = new Size(43, 17);
        labelPriority.TabIndex = 15;
        labelPriority.Text = "优先级";
        // 
        // numPriority
        // 
        numPriority.Dock = DockStyle.Top;
        numPriority.Location = new Point(47, 298);
        numPriority.Margin = new Padding(0, 4, 0, 4);
        numPriority.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        numPriority.Name = "numPriority";
        numPriority.Size = new Size(276, 27);
        numPriority.TabIndex = 16;
        numPriority.ValueChanged += Field_ValueChanged;
        // 
        // labelEnabled
        // 
        labelEnabled.Anchor = AnchorStyles.Left;
        labelEnabled.AutoSize = true;
        labelEnabled.Location = new Point(8, 338);
        labelEnabled.Margin = new Padding(0, 6, 8, 6);
        labelEnabled.Name = "labelEnabled";
        labelEnabled.Size = new Size(31, 17);
        labelEnabled.TabIndex = 17;
        labelEnabled.Text = "启用";
        // 
        // chkEnabled
        // 
        chkEnabled.AutoSize = true;
        chkEnabled.Checked = true;
        chkEnabled.CheckState = CheckState.Checked;
        chkEnabled.Dock = DockStyle.Top;
        chkEnabled.Location = new Point(47, 338);
        chkEnabled.Margin = new Padding(0, 6, 0, 6);
        chkEnabled.Name = "chkEnabled";
        chkEnabled.Size = new Size(276, 21);
        chkEnabled.TabIndex = 18;
        chkEnabled.Text = "启用";
        chkEnabled.UseVisualStyleBackColor = true;
        chkEnabled.CheckedChanged += Field_CheckedChanged;
        // 
        // bottom
        // 
        bottom.AutoSize = true;
        bottom.Controls.Add(btnAdd);
        bottom.Controls.Add(btnDelete);
        bottom.Controls.Add(btnUp);
        bottom.Controls.Add(btnDown);
        bottom.Controls.Add(btnApply);
        bottom.Controls.Add(btnSuggest);
        bottom.Controls.Add(btnOk);
        bottom.Controls.Add(btnCancel);
        bottom.Dock = DockStyle.Bottom;
        bottom.FlowDirection = FlowDirection.LeftToRight;
        bottom.Location = new Point(0, 511);
        bottom.Name = "bottom";
        bottom.Padding = new Padding(8);
        bottom.Size = new Size(1000, 49);
        bottom.TabIndex = 2;
        bottom.WrapContents = false;
        // 
        // btnAdd
        // 
        btnAdd.Location = new Point(11, 11);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(70, 23);
        btnAdd.TabIndex = 0;
        btnAdd.Text = "新增";
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += ButtonAdd_Click;
        // 
        // btnDelete
        // 
        btnDelete.Location = new Point(87, 11);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(70, 23);
        btnDelete.TabIndex = 1;
        btnDelete.Text = "删除";
        btnDelete.UseVisualStyleBackColor = true;
        btnDelete.Click += ButtonDelete_Click;
        // 
        // btnUp
        // 
        btnUp.Location = new Point(163, 11);
        btnUp.Name = "btnUp";
        btnUp.Size = new Size(60, 23);
        btnUp.TabIndex = 2;
        btnUp.Text = "上移";
        btnUp.UseVisualStyleBackColor = true;
        btnUp.Click += ButtonUp_Click;
        // 
        // btnDown
        // 
        btnDown.Location = new Point(229, 11);
        btnDown.Name = "btnDown";
        btnDown.Size = new Size(60, 23);
        btnDown.TabIndex = 3;
        btnDown.Text = "下移";
        btnDown.UseVisualStyleBackColor = true;
        btnDown.Click += ButtonDown_Click;
        // 
        // btnApply
        // 
        btnApply.Location = new Point(295, 11);
        btnApply.Name = "btnApply";
        btnApply.Size = new Size(90, 23);
        btnApply.TabIndex = 4;
        btnApply.Text = "应用修改";
        btnApply.UseVisualStyleBackColor = true;
        btnApply.Click += ButtonApply_Click;
        // 
        // btnSuggest
        // 
        btnSuggest.Location = new Point(391, 11);
        btnSuggest.Name = "btnSuggest";
        btnSuggest.Size = new Size(110, 23);
        btnSuggest.TabIndex = 5;
        btnSuggest.Text = "由手动匹配归纳";
        btnSuggest.UseVisualStyleBackColor = true;
        btnSuggest.Click += ButtonSuggest_Click;
        // 
        // btnClearRules
        // 
        btnClearRules.Location = new Point(507, 11);
        btnClearRules.Name = "btnClearRules";
        btnClearRules.Size = new Size(80, 23);
        btnClearRules.TabIndex = 6;
        btnClearRules.Text = "清空规则";
        btnClearRules.UseVisualStyleBackColor = true;
        btnClearRules.Click += ButtonClearRules_Click;
        // 
        // btnReset
        // 
        btnReset.Location = new Point(593, 11);
        btnReset.Name = "btnReset";
        btnReset.Size = new Size(110, 23);
        btnReset.TabIndex = 7;
        btnReset.Text = "重置本文件…";
        btnReset.UseVisualStyleBackColor = true;
        btnReset.Click += ButtonReset_Click;
        // 
        // btnOk
        // 
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(709, 11);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(100, 23);
        btnOk.TabIndex = 8;
        btnOk.Text = "保存并关闭";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += ButtonOk_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(815, 11);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(70, 23);
        btnCancel.TabIndex = 9;
        btnCancel.Text = "取消";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // previewTimer
        // 
        previewTimer.Interval = 200;
        previewTimer.Tick += PreviewTimer_Tick;
        // 
        // PdfAudioRuleDialog
        // 
        AcceptButton = btnOk;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(1000, 604);
        Controls.Add(splitContainer);
        Controls.Add(editPanel);
        Controls.Add(bottom);
        Controls.Add(topBar);
        MinimumSize = new Size(900, 520);
        Name = "PdfAudioRuleDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "音频匹配规则";
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        editPanel.ResumeLayout(false);
        editPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numOffset).EndInit();
        ((System.ComponentModel.ISupportInitialize)numPagesPerUnit).EndInit();
        ((System.ComponentModel.ISupportInitialize)numPriority).EndInit();
        bottom.ResumeLayout(false);
        topBar.ResumeLayout(false);
        topBar.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private FlowLayoutPanel topBar;
    private Label labelMatchMode;
    private ComboBox cmbMatchMode;
    private Label labelSingleAudio;
    private Button btnPickSingle;
    private SplitContainer splitContainer;
    private ListView lvRules;
    private ColumnHeader columnName;
    private ColumnHeader columnMode;
    private ColumnHeader columnPriority;
    private ColumnHeader columnEnabled;
    private ColumnHeader columnPattern;
    private ListBox lstPreview;
    private Label labelDiagnostics;
    private Label labelPreviewSummary;
    private TableLayoutPanel editPanel;
    private Label labelName;
    private TextBox txtName;
    private Label labelPattern;
    private TextBox txtPattern;
    private Label labelSyntax;
    private ComboBox cmbSyntax;
    private Label labelModeHint;
    private Label labelMode;
    private ComboBox cmbMode;
    private Label labelCapture;
    private TextBox txtCapture;
    private Label labelOffset;
    private NumericUpDown numOffset;
    private Label labelPagesPerUnit;
    private NumericUpDown numPagesPerUnit;
    private Label labelPriority;
    private NumericUpDown numPriority;
    private Label labelEnabled;
    private CheckBox chkEnabled;
    private FlowLayoutPanel bottom;
    private Button btnAdd;
    private Button btnDelete;
    private Button btnUp;
    private Button btnDown;
    private Button btnApply;
    private Button btnSuggest;
    private Button btnClearRules;
    private Button btnReset;
    private Button btnOk;
    private Button btnCancel;
    private ToolTip tooltip;
    private System.Windows.Forms.Timer previewTimer;
}
