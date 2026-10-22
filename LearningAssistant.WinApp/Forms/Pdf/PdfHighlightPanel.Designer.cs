using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfHighlightPanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        header = new SidebarHeaderBar();
        colors = new FlowLayoutPanel();
        colorHint = new Label();
        yellowRadio = new RadioButton();
        greenRadio = new RadioButton();
        blueRadio = new RadioButton();
        pinkRadio = new RadioButton();
        orangeRadio = new RadioButton();
        listHost = new Panel();
        highlightList = new ListBox();
        footer = new FlowLayoutPanel();
        removeButton = new Button();
        editButton = new Button();
        batchRemoveButton = new Button();
        _tip = new ToolTip(components);
        root.SuspendLayout();
        colors.SuspendLayout();
        listHost.SuspendLayout();
        footer.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(colors, 0, 1);
        root.Controls.Add(listHost, 0, 2);
        root.Controls.Add(footer, 0, 3);
        root.Dock = DockStyle.Fill;
        root.Margin = Padding.Empty;
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 6);
        root.RowCount = 4;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.Size = new Size(288, 600);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.ActionText = "导出";
        header.Dock = DockStyle.Fill;
        header.Icon = "📑";
        header.Margin = Padding.Empty;
        header.Name = "header";
        header.Size = new Size(276, 48);
        header.TabIndex = 0;
        header.TitleText = "高亮";
        // 
        // colors
        // 
        colors.BackColor = Color.FromArgb(247, 248, 251);
        colors.Controls.Add(yellowRadio);
        colors.Controls.Add(greenRadio);
        colors.Controls.Add(blueRadio);
        colors.Controls.Add(pinkRadio);
        colors.Controls.Add(orangeRadio);
        colors.Controls.Add(colorHint);
        colors.Dock = DockStyle.Fill;
        colors.FlowDirection = FlowDirection.LeftToRight;
        colors.Margin = new Padding(0, 2, 0, 2);
        colors.Name = "colors";
        colors.Padding = new Padding(2, 4, 0, 0);
        colors.Size = new Size(276, 32);
        colors.TabIndex = 1;
        colors.WrapContents = false;
        // 
        // colorHint
        // 
        colorHint.AutoSize = true;
        colorHint.Font = new Font("Microsoft YaHei UI", 8.5F);
        colorHint.ForeColor = Color.FromArgb(100, 116, 139);
        colorHint.Margin = new Padding(6, 6, 0, 0);
        colorHint.Name = "colorHint";
        colorHint.Size = new Size(98, 19);
        colorHint.TabIndex = 5;
        colorHint.Text = "选色后框选文字";
        // 
        // yellowRadio
        // 
        yellowRadio.Appearance = Appearance.Button;
        yellowRadio.BackColor = Color.FromArgb(253, 224, 71);
        yellowRadio.Checked = true;
        yellowRadio.Cursor = Cursors.Hand;
        yellowRadio.FlatAppearance.BorderColor = Color.White;
        yellowRadio.FlatAppearance.BorderSize = 2;
        yellowRadio.FlatAppearance.CheckedBackColor = Color.FromArgb(253, 224, 71);
        yellowRadio.FlatStyle = FlatStyle.Flat;
        yellowRadio.Margin = new Padding(2, 0, 6, 0);
        yellowRadio.Name = "yellowRadio";
        yellowRadio.Size = new Size(24, 24);
        yellowRadio.TabIndex = 0;
        yellowRadio.TabStop = true;
        yellowRadio.Tag = "keep";
        yellowRadio.UseVisualStyleBackColor = false;
        // 
        // greenRadio
        // 
        greenRadio.Appearance = Appearance.Button;
        greenRadio.BackColor = Color.FromArgb(74, 222, 128);
        greenRadio.Cursor = Cursors.Hand;
        greenRadio.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        greenRadio.FlatAppearance.CheckedBackColor = Color.FromArgb(74, 222, 128);
        greenRadio.FlatStyle = FlatStyle.Flat;
        greenRadio.Margin = new Padding(2, 0, 6, 0);
        greenRadio.Name = "greenRadio";
        greenRadio.Size = new Size(24, 24);
        greenRadio.TabIndex = 1;
        greenRadio.Tag = "keep";
        greenRadio.UseVisualStyleBackColor = false;
        // 
        // blueRadio
        // 
        blueRadio.Appearance = Appearance.Button;
        blueRadio.BackColor = Color.FromArgb(96, 165, 250);
        blueRadio.Cursor = Cursors.Hand;
        blueRadio.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        blueRadio.FlatAppearance.CheckedBackColor = Color.FromArgb(96, 165, 250);
        blueRadio.FlatStyle = FlatStyle.Flat;
        blueRadio.Margin = new Padding(2, 0, 6, 0);
        blueRadio.Name = "blueRadio";
        blueRadio.Size = new Size(24, 24);
        blueRadio.TabIndex = 2;
        blueRadio.Tag = "keep";
        blueRadio.UseVisualStyleBackColor = false;
        // 
        // pinkRadio
        // 
        pinkRadio.Appearance = Appearance.Button;
        pinkRadio.BackColor = Color.FromArgb(244, 114, 182);
        pinkRadio.Cursor = Cursors.Hand;
        pinkRadio.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        pinkRadio.FlatAppearance.CheckedBackColor = Color.FromArgb(244, 114, 182);
        pinkRadio.FlatStyle = FlatStyle.Flat;
        pinkRadio.Margin = new Padding(2, 0, 6, 0);
        pinkRadio.Name = "pinkRadio";
        pinkRadio.Size = new Size(24, 24);
        pinkRadio.TabIndex = 3;
        pinkRadio.Tag = "keep";
        pinkRadio.UseVisualStyleBackColor = false;
        // 
        // orangeRadio
        // 
        orangeRadio.Appearance = Appearance.Button;
        orangeRadio.BackColor = Color.FromArgb(251, 146, 60);
        orangeRadio.Cursor = Cursors.Hand;
        orangeRadio.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        orangeRadio.FlatAppearance.CheckedBackColor = Color.FromArgb(251, 146, 60);
        orangeRadio.FlatStyle = FlatStyle.Flat;
        orangeRadio.Margin = new Padding(2, 0, 6, 0);
        orangeRadio.Name = "orangeRadio";
        orangeRadio.Size = new Size(24, 24);
        orangeRadio.TabIndex = 4;
        orangeRadio.Tag = "keep";
        orangeRadio.UseVisualStyleBackColor = false;
        // 
        // listHost
        // 
        listHost.BackColor = Color.White;
        listHost.Controls.Add(highlightList);
        listHost.Dock = DockStyle.Fill;
        listHost.Margin = new Padding(0, 0, 0, 2);
        listHost.Name = "listHost";
        listHost.Size = new Size(276, 464);
        listHost.TabIndex = 2;
        listHost.Tag = "surface";
        // 
        // highlightList
        // 
        highlightList.BackColor = Color.White;
        highlightList.BorderStyle = BorderStyle.None;
        highlightList.Dock = DockStyle.Fill;
        highlightList.DrawMode = DrawMode.OwnerDrawFixed;
        highlightList.Font = new Font("Microsoft YaHei UI", 9F);
        highlightList.ForeColor = Color.FromArgb(15, 23, 42);
        highlightList.FormattingEnabled = true;
        highlightList.IntegralHeight = false;
        highlightList.ItemHeight = 52;
        highlightList.Name = "highlightList";
        highlightList.Size = new Size(276, 464);
        highlightList.TabIndex = 0;
        // 
        // footer
        // 
        footer.BackColor = Color.FromArgb(247, 248, 251);
        footer.Controls.Add(removeButton);
        footer.Controls.Add(editButton);
        footer.Controls.Add(batchRemoveButton);
        footer.Dock = DockStyle.Fill;
        footer.FlowDirection = FlowDirection.LeftToRight;
        footer.Margin = Padding.Empty;
        footer.Name = "footer";
        footer.Padding = new Padding(0, 4, 0, 0);
        footer.Size = new Size(276, 36);
        footer.TabIndex = 3;
        footer.WrapContents = false;
        // 
        // removeButton
        // 
        removeButton.Cursor = Cursors.Hand;
        removeButton.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
        removeButton.FlatStyle = FlatStyle.Flat;
        removeButton.Margin = new Padding(0, 0, 6, 0);
        removeButton.Name = "removeButton";
        removeButton.Size = new Size(72, 28);
        removeButton.TabIndex = 0;
        removeButton.Text = "删除";
        removeButton.UseVisualStyleBackColor = false;
        // 
        // editButton
        // 
        editButton.Cursor = Cursors.Hand;
        editButton.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
        editButton.FlatStyle = FlatStyle.Flat;
        editButton.Margin = new Padding(0, 0, 6, 0);
        editButton.Name = "editButton";
        editButton.Size = new Size(72, 28);
        editButton.TabIndex = 1;
        editButton.Text = "编辑";
        editButton.UseVisualStyleBackColor = false;
        // 
        // batchRemoveButton
        // 
        batchRemoveButton.Cursor = Cursors.Hand;
        batchRemoveButton.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
        batchRemoveButton.FlatStyle = FlatStyle.Flat;
        batchRemoveButton.Margin = new Padding(0);
        batchRemoveButton.Name = "batchRemoveButton";
        batchRemoveButton.Size = new Size(92, 28);
        batchRemoveButton.TabIndex = 2;
        batchRemoveButton.Text = "批量删除";
        batchRemoveButton.UseVisualStyleBackColor = false;
        // 
        // PdfHighlightPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfHighlightPanel";
        Size = new Size(300, 612);
        root.ResumeLayout(false);
        root.PerformLayout();
        colors.ResumeLayout(false);
        colors.PerformLayout();
        listHost.ResumeLayout(false);
        footer.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SidebarHeaderBar header;
    private FlowLayoutPanel colors;
    private Label colorHint;
    private RadioButton yellowRadio;
    private RadioButton greenRadio;
    private RadioButton blueRadio;
    private RadioButton pinkRadio;
    private RadioButton orangeRadio;
    private Panel listHost;
    private ListBox highlightList;
    private FlowLayoutPanel footer;
    private Button removeButton;
    private Button editButton;
    private Button batchRemoveButton;
    private ToolTip _tip;
}
