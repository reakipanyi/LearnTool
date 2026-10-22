namespace LearningAssistant.Forms.Pdf;

partial class PdfAudioAiMatchDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        grid = new DataGridView();
        columnSelect = new DataGridViewCheckBoxColumn();
        columnAudio = new DataGridViewTextBoxColumn();
        columnPage = new DataGridViewTextBoxColumn();
        columnConfidence = new DataGridViewTextBoxColumn();
        columnReason = new DataGridViewTextBoxColumn();
        buttonsPanel = new FlowLayoutPanel();
        btnCancel = new Button();
        btnOk = new Button();
        btnSelectNone = new Button();
        btnSelectAll = new Button();
        labelSummary = new Label();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // grid
        // 
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Columns.AddRange(new DataGridViewColumn[] { columnSelect, columnAudio, columnPage, columnConfidence, columnReason });
        grid.Dock = DockStyle.Fill;
        grid.Location = new Point(0, 44);
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.Size = new Size(760, 390);
        grid.TabIndex = 0;
        // 
        // columnSelect
        // 
        columnSelect.FalseValue = false;
        columnSelect.FillWeight = 50F;
        columnSelect.HeaderText = "选择";
        columnSelect.Name = "columnSelect";
        columnSelect.SortMode = DataGridViewColumnSortMode.NotSortable;
        columnSelect.TrueValue = true;
        columnSelect.Width = 50;
        // 
        // columnAudio
        // 
        columnAudio.FillWeight = 220F;
        columnAudio.HeaderText = "音频文件";
        columnAudio.Name = "columnAudio";
        columnAudio.ReadOnly = true;
        columnAudio.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // columnPage
        // 
        columnPage.FillWeight = 60F;
        columnPage.HeaderText = "页码";
        columnPage.Name = "columnPage";
        columnPage.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // columnConfidence
        // 
        columnConfidence.FillWeight = 60F;
        columnConfidence.HeaderText = "置信度";
        columnConfidence.Name = "columnConfidence";
        columnConfidence.ReadOnly = true;
        columnConfidence.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // columnReason
        // 
        columnReason.FillWeight = 280F;
        columnReason.HeaderText = "AI 依据";
        columnReason.Name = "columnReason";
        columnReason.ReadOnly = true;
        columnReason.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // buttonsPanel
        // 
        buttonsPanel.Controls.Add(btnCancel);
        buttonsPanel.Controls.Add(btnOk);
        buttonsPanel.Controls.Add(btnSelectNone);
        buttonsPanel.Controls.Add(btnSelectAll);
        buttonsPanel.Dock = DockStyle.Bottom;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(0, 434);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Padding = new Padding(8);
        buttonsPanel.Size = new Size(760, 46);
        buttonsPanel.TabIndex = 1;
        buttonsPanel.WrapContents = false;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(676, 11);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(76, 30);
        btnCancel.TabIndex = 0;
        btnCancel.Text = "取消";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // btnOk
        // 
        btnOk.Location = new Point(572, 11);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(96, 30);
        btnOk.TabIndex = 1;
        btnOk.Text = "应用匹配";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += ButtonOk_Click;
        // 
        // btnSelectNone
        // 
        btnSelectNone.Location = new Point(488, 11);
        btnSelectNone.Name = "btnSelectNone";
        btnSelectNone.Size = new Size(76, 30);
        btnSelectNone.TabIndex = 2;
        btnSelectNone.Text = "全不选";
        btnSelectNone.UseVisualStyleBackColor = true;
        btnSelectNone.Click += ButtonSelectNone_Click;
        // 
        // btnSelectAll
        // 
        btnSelectAll.Location = new Point(404, 11);
        btnSelectAll.Name = "btnSelectAll";
        btnSelectAll.Size = new Size(76, 30);
        btnSelectAll.TabIndex = 3;
        btnSelectAll.Text = "全选";
        btnSelectAll.UseVisualStyleBackColor = true;
        btnSelectAll.Click += ButtonSelectAll_Click;
        // 
        // labelSummary
        // 
        labelSummary.AutoSize = true;
        labelSummary.Dock = DockStyle.Top;
        labelSummary.Location = new Point(0, 0);
        labelSummary.Name = "labelSummary";
        labelSummary.Padding = new Padding(10, 10, 10, 4);
        labelSummary.Size = new Size(190, 31);
        labelSummary.TabIndex = 2;
        labelSummary.Text = "AI 给出 0 条建议，可修改页码或取消勾选后应用。";
        // 
        // PdfAudioAiMatchDialog
        // 
        AcceptButton = btnOk;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(760, 480);
        Controls.Add(grid);
        Controls.Add(buttonsPanel);
        Controls.Add(labelSummary);
        Font = new Font("Microsoft YaHei UI", 9F);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PdfAudioAiMatchDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "AI 自动匹配预览";
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        buttonsPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private DataGridView grid;
    private DataGridViewCheckBoxColumn columnSelect;
    private DataGridViewTextBoxColumn columnAudio;
    private DataGridViewTextBoxColumn columnPage;
    private DataGridViewTextBoxColumn columnConfidence;
    private DataGridViewTextBoxColumn columnReason;
    private FlowLayoutPanel buttonsPanel;
    private Button btnCancel;
    private Button btnOk;
    private Button btnSelectNone;
    private Button btnSelectAll;
    private Label labelSummary;
}
