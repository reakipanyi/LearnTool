namespace LearningAssistant.Forms.Pdf;

partial class PdfSearchDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        labelSearch = new Label();
        textBoxSearch = new TextBox();
        buttonFind = new Button();
        checkCase = new CheckBox();
        checkWhole = new CheckBox();
        SuspendLayout();
        // 
        // labelSearch
        // 
        labelSearch.AutoSize = true;
        labelSearch.Location = new Point(15, 20);
        labelSearch.Name = "labelSearch";
        labelSearch.Size = new Size(64, 17);
        labelSearch.TabIndex = 0;
        labelSearch.Text = "查找内容:";
        // 
        // textBoxSearch
        // 
        textBoxSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        textBoxSearch.Location = new Point(85, 18);
        textBoxSearch.Name = "textBoxSearch";
        textBoxSearch.Size = new Size(200, 24);
        textBoxSearch.TabIndex = 1;
        // 
        // buttonFind
        // 
        buttonFind.Location = new Point(295, 15);
        buttonFind.Name = "buttonFind";
        buttonFind.Size = new Size(80, 30);
        buttonFind.TabIndex = 2;
        buttonFind.Text = "查找";
        buttonFind.UseVisualStyleBackColor = true;
        // 
        // checkCase
        // 
        checkCase.AutoSize = true;
        checkCase.Location = new Point(15, 55);
        checkCase.Name = "checkCase";
        checkCase.Size = new Size(86, 21);
        checkCase.TabIndex = 3;
        checkCase.Tag = "searchCase";
        checkCase.Text = "区分大小写";
        checkCase.UseVisualStyleBackColor = true;
        // 
        // checkWhole
        // 
        checkWhole.AutoSize = true;
        checkWhole.Location = new Point(120, 55);
        checkWhole.Name = "checkWhole";
        checkWhole.Size = new Size(82, 21);
        checkWhole.TabIndex = 4;
        checkWhole.Tag = "searchWhole";
        checkWhole.Text = "全字匹配";
        checkWhole.UseVisualStyleBackColor = true;
        // 
        // PdfSearchDialog
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(384, 111);
        Controls.Add(checkWhole);
        Controls.Add(checkCase);
        Controls.Add(buttonFind);
        Controls.Add(textBoxSearch);
        Controls.Add(labelSearch);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "PdfSearchDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "查找";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label labelSearch;
    private TextBox textBoxSearch;
    private Button buttonFind;
    private CheckBox checkCase;
    private CheckBox checkWhole;
}
