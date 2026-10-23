namespace LearningAssistant.Forms.Pdf;

partial class PdfTranslationGlossaryDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        labelSummary = new Label();
        grid = new DataGridView();
        columnSource = new DataGridViewTextBoxColumn();
        columnTarget = new DataGridViewTextBoxColumn();
        columnHit = new DataGridViewTextBoxColumn();
        buttonsPanel = new FlowLayoutPanel();
        btnCancel = new Button();
        btnSave = new Button();
        btnDelete = new Button();
        btnAdd = new Button();
        ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
        buttonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // labelSummary
        // 
        labelSummary.Dock = DockStyle.Top;
        labelSummary.Location = new Point(0, 0);
        labelSummary.Name = "labelSummary";
        labelSummary.Padding = new Padding(12, 10, 12, 6);
        labelSummary.Size = new Size(720, 44);
        labelSummary.TabIndex = 0;
        // 
        // grid
        // 
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = SystemColors.Control;
        dataGridViewCellStyle1.Font = new Font("Microsoft YaHei UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.Columns.AddRange(new DataGridViewColumn[] { columnSource, columnTarget, columnHit });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = SystemColors.Window;
        dataGridViewCellStyle2.Font = new Font("Microsoft YaHei UI", 9F);
        dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle = dataGridViewCellStyle2;
        grid.Dock = DockStyle.Fill;
        grid.Location = new Point(0, 44);
        grid.MultiSelect = true;
        grid.Name = "grid";
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.Size = new Size(720, 386);
        grid.TabIndex = 1;
        grid.CellEndEdit += Grid_CellEndEdit;
        // 
        // columnSource
        // 
        columnSource.FillWeight = 45F;
        columnSource.HeaderText = "原文术语";
        columnSource.Name = "columnSource";
        columnSource.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // columnTarget
        // 
        columnTarget.FillWeight = 45F;
        columnTarget.HeaderText = "统一译名";
        columnTarget.Name = "columnTarget";
        columnTarget.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // columnHit
        // 
        columnHit.FillWeight = 10F;
        columnHit.HeaderText = "本页命中";
        columnHit.Name = "columnHit";
        columnHit.ReadOnly = true;
        columnHit.SortMode = DataGridViewColumnSortMode.NotSortable;
        // 
        // buttonsPanel
        // 
        buttonsPanel.Controls.Add(btnCancel);
        buttonsPanel.Controls.Add(btnSave);
        buttonsPanel.Controls.Add(btnDelete);
        buttonsPanel.Controls.Add(btnAdd);
        buttonsPanel.Dock = DockStyle.Bottom;
        buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonsPanel.Location = new Point(0, 430);
        buttonsPanel.Name = "buttonsPanel";
        buttonsPanel.Padding = new Padding(8);
        buttonsPanel.Size = new Size(720, 46);
        buttonsPanel.TabIndex = 2;
        buttonsPanel.WrapContents = false;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(632, 11);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(76, 30);
        btnCancel.TabIndex = 0;
        btnCancel.Text = "取消";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // btnSave
        // 
        btnSave.Location = new Point(530, 11);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(96, 30);
        btnSave.TabIndex = 1;
        btnSave.Text = "保存";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += ButtonSave_Click;
        // 
        // btnDelete
        // 
        btnDelete.Location = new Point(448, 11);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(76, 30);
        btnDelete.TabIndex = 2;
        btnDelete.Text = "删除所选";
        btnDelete.UseVisualStyleBackColor = true;
        btnDelete.Click += ButtonDelete_Click;
        // 
        // btnAdd
        // 
        btnAdd.Location = new Point(366, 11);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(76, 30);
        btnAdd.TabIndex = 3;
        btnAdd.Text = "添加术语";
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += ButtonAdd_Click;
        // 
        // PdfTranslationGlossaryDialog
        // 
        AcceptButton = btnSave;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(720, 476);
        Controls.Add(grid);
        Controls.Add(buttonsPanel);
        Controls.Add(labelSummary);
        Font = new Font("Microsoft YaHei UI", 9F);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PdfTranslationGlossaryDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "翻译术语表";
        ((System.ComponentModel.ISupportInitialize)grid).EndInit();
        buttonsPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label labelSummary;
    private DataGridView grid;
    private DataGridViewTextBoxColumn columnSource;
    private DataGridViewTextBoxColumn columnTarget;
    private DataGridViewTextBoxColumn columnHit;
    private FlowLayoutPanel buttonsPanel;
    private Button btnCancel;
    private Button btnSave;
    private Button btnDelete;
    private Button btnAdd;
}
