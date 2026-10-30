namespace LearningAssistant.Forms.Pdf;

partial class PdfAiAnswerDialog
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        panelAnswer = new Panel();
        textAnswer = new RichTextBox();
        panelBottom = new Panel();
        labelPages = new Label();
        flowPages = new FlowLayoutPanel();
        buttonClose = new Button();
        labelTitle = new Label();
        labelMeta = new Label();
        panelAnswer.SuspendLayout();
        panelBottom.SuspendLayout();
        SuspendLayout();
        // 
        // panelAnswer
        // 
        panelAnswer.Dock = DockStyle.Fill;
        panelAnswer.Location = new Point(0, 46);
        panelAnswer.Name = "panelAnswer";
        panelAnswer.Padding = new Padding(14, 4, 14, 10);
        panelAnswer.Size = new Size(664, 456);
        panelAnswer.TabIndex = 0;
        // 
        // textAnswer
        // 
        textAnswer.BackColor = Color.White;
        textAnswer.BorderStyle = BorderStyle.None;
        textAnswer.DetectUrls = false;
        textAnswer.Dock = DockStyle.Fill;
        textAnswer.Font = new Font("微软雅黑", 10F);
        textAnswer.ForeColor = Color.FromArgb(40, 40, 40);
        textAnswer.Location = new Point(14, 4);
        textAnswer.Name = "textAnswer";
        textAnswer.ReadOnly = true;
        textAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
        textAnswer.Size = new Size(636, 442);
        textAnswer.TabIndex = 0;
        textAnswer.Text = "AI 回答内容将显示在这里。";
        // 
        // panelBottom
        // 
        panelBottom.BackColor = Color.FromArgb(248, 248, 248);
        panelBottom.Controls.Add(labelPages);
        panelBottom.Controls.Add(flowPages);
        panelBottom.Controls.Add(buttonClose);
        panelBottom.Dock = DockStyle.Bottom;
        panelBottom.Location = new Point(0, 502);
        panelBottom.Name = "panelBottom";
        panelBottom.Padding = new Padding(12, 10, 12, 10);
        panelBottom.Size = new Size(664, 58);
        panelBottom.TabIndex = 1;
        // 
        // labelPages
        // 
        labelPages.AutoSize = true;
        labelPages.ForeColor = Color.FromArgb(90, 90, 90);
        labelPages.Location = new Point(12, 18);
        labelPages.Name = "labelPages";
        labelPages.Size = new Size(68, 17);
        labelPages.TabIndex = 0;
        labelPages.Text = "引用页码：";
        // 
        // flowPages
        // 
        flowPages.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        flowPages.AutoScroll = true;
        flowPages.Location = new Point(86, 12);
        flowPages.Name = "flowPages";
        flowPages.Size = new Size(440, 36);
        flowPages.TabIndex = 1;
        flowPages.WrapContents = false;
        // 
        // buttonClose
        // 
        buttonClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonClose.BackColor = Color.FromArgb(139, 92, 246);
        buttonClose.Cursor = Cursors.Hand;
        buttonClose.FlatAppearance.BorderSize = 0;
        buttonClose.FlatStyle = FlatStyle.Flat;
        buttonClose.ForeColor = Color.White;
        buttonClose.Location = new Point(568, 12);
        buttonClose.Name = "buttonClose";
        buttonClose.Size = new Size(84, 32);
        buttonClose.TabIndex = 2;
        buttonClose.Text = "关闭";
        buttonClose.UseVisualStyleBackColor = false;
        buttonClose.Click += ButtonClose_Click;
        // 
        // labelTitle
        // 
        labelTitle.Dock = DockStyle.Top;
        labelTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
        labelTitle.ForeColor = Color.FromArgb(60, 60, 60);
        labelTitle.Location = new Point(0, 0);
        labelTitle.Name = "labelTitle";
        labelTitle.Padding = new Padding(14, 12, 14, 6);
        labelTitle.Size = new Size(664, 46);
        labelTitle.TabIndex = 2;
        labelTitle.Text = "AI 问答";
        // 
        // labelMeta
        // 
        labelMeta.Dock = DockStyle.Top;
        labelMeta.Font = new Font("微软雅黑", 8.5F);
        labelMeta.ForeColor = Color.FromArgb(120, 120, 120);
        labelMeta.Location = new Point(0, 46);
        labelMeta.Name = "labelMeta";
        labelMeta.Padding = new Padding(14, 0, 14, 0);
        labelMeta.Size = new Size(664, 22);
        labelMeta.TabIndex = 3;
        labelMeta.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // PdfAiAnswerDialog
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(664, 560);
        Controls.Add(panelAnswer);
        Controls.Add(panelBottom);
        Controls.Add(labelMeta);
        Controls.Add(labelTitle);
        Font = new Font("微软雅黑", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        KeyPreview = true;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(520, 420);
        Name = "PdfAiAnswerDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "AI 问答";
        KeyDown += PdfAiAnswerDialog_KeyDown;
        panelAnswer.ResumeLayout(false);
        panelBottom.ResumeLayout(false);
        panelBottom.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel panelAnswer;
    private RichTextBox textAnswer;
    private Panel panelBottom;
    private Label labelPages;
    private FlowLayoutPanel flowPages;
    private Button buttonClose;
    private Label labelTitle;
    private Label labelMeta;
}
