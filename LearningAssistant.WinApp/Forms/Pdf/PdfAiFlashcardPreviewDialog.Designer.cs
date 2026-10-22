namespace LearningAssistant.Forms.Pdf;

partial class PdfAiFlashcardPreviewDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        accentBar = new Panel();
        labelTitle = new Label();
        labelDoc = new Label();
        listCards = new CheckedListBox();
        buttonSelectAll = new Button();
        buttonSelectNone = new Button();
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
        accentBar.Size = new Size(640, 4);
        accentBar.TabIndex = 0;
        // 
        // labelTitle
        // 
        labelTitle.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        labelTitle.ForeColor = Color.FromArgb(50, 50, 60);
        labelTitle.Location = new Point(20, 16);
        labelTitle.Name = "labelTitle";
        labelTitle.Size = new Size(420, 26);
        labelTitle.TabIndex = 1;
        labelTitle.Text = "AI 闪卡预览（0 张）";
        // 
        // labelDoc
        // 
        labelDoc.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        labelDoc.AutoEllipsis = true;
        labelDoc.ForeColor = Color.FromArgb(140, 140, 152);
        labelDoc.Location = new Point(380, 20);
        labelDoc.Name = "labelDoc";
        labelDoc.Size = new Size(240, 22);
        labelDoc.TabIndex = 2;
        labelDoc.TextAlign = ContentAlignment.MiddleRight;
        // 
        // listCards
        // 
        listCards.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        listCards.BorderStyle = BorderStyle.FixedSingle;
        listCards.CheckOnClick = true;
        listCards.IntegralHeight = false;
        listCards.Location = new Point(20, 52);
        listCards.Name = "listCards";
        listCards.Size = new Size(600, 382);
        listCards.TabIndex = 3;
        // 
        // buttonSelectAll
        // 
        buttonSelectAll.BackColor = Color.White;
        buttonSelectAll.Cursor = Cursors.Hand;
        buttonSelectAll.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
        buttonSelectAll.FlatStyle = FlatStyle.Flat;
        buttonSelectAll.Location = new Point(20, 448);
        buttonSelectAll.Name = "buttonSelectAll";
        buttonSelectAll.Size = new Size(80, 32);
        buttonSelectAll.TabIndex = 4;
        buttonSelectAll.Text = "全选";
        buttonSelectAll.UseVisualStyleBackColor = false;
        buttonSelectAll.Click += ButtonSelectAll_Click;
        // 
        // buttonSelectNone
        // 
        buttonSelectNone.BackColor = Color.White;
        buttonSelectNone.Cursor = Cursors.Hand;
        buttonSelectNone.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
        buttonSelectNone.FlatStyle = FlatStyle.Flat;
        buttonSelectNone.Location = new Point(108, 448);
        buttonSelectNone.Name = "buttonSelectNone";
        buttonSelectNone.Size = new Size(80, 32);
        buttonSelectNone.TabIndex = 5;
        buttonSelectNone.Text = "全不选";
        buttonSelectNone.UseVisualStyleBackColor = false;
        buttonSelectNone.Click += ButtonSelectNone_Click;
        // 
        // buttonOk
        // 
        buttonOk.BackColor = Color.FromArgb(139, 92, 246);
        buttonOk.Cursor = Cursors.Hand;
        buttonOk.FlatAppearance.BorderSize = 0;
        buttonOk.FlatStyle = FlatStyle.Flat;
        buttonOk.ForeColor = Color.White;
        buttonOk.Location = new Point(408, 448);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(126, 32);
        buttonOk.TabIndex = 6;
        buttonOk.Text = "加入闪卡复习";
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
        buttonCancel.Location = new Point(540, 448);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(80, 32);
        buttonCancel.TabIndex = 7;
        buttonCancel.Text = "取消";
        buttonCancel.UseVisualStyleBackColor = false;
        buttonCancel.Click += ButtonCancel_Click;
        // 
        // PdfAiFlashcardPreviewDialog
        // 
        AcceptButton = buttonOk;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        CancelButton = buttonCancel;
        ClientSize = new Size(640, 520);
        Controls.Add(buttonCancel);
        Controls.Add(buttonOk);
        Controls.Add(buttonSelectNone);
        Controls.Add(buttonSelectAll);
        Controls.Add(listCards);
        Controls.Add(labelDoc);
        Controls.Add(labelTitle);
        Controls.Add(accentBar);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(520, 400);
        MinimizeBox = false;
        Name = "PdfAiFlashcardPreviewDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "AI 闪卡预览";
        ResumeLayout(false);
    }

    #endregion

    private Panel accentBar;
    private Label labelTitle;
    private Label labelDoc;
    private CheckedListBox listCards;
    private Button buttonSelectAll;
    private Button buttonSelectNone;
    private Button buttonOk;
    private Button buttonCancel;
}
