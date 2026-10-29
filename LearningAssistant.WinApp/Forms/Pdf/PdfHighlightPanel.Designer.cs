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
        optionsBar = new Panel();
        listHost = new Panel();
        highlightList = new ListBox();
        mergeMultiLineCheck = new CheckBox();
        removeButton = new Button();
        editButton = new Button();
        batchRemoveButton = new Button();
        autoHighlightButton = new Button();
        _tip = new ToolTip(components);
        root.SuspendLayout();
        optionsBar.SuspendLayout();
        listHost.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(optionsBar, 0, 1);
        root.Controls.Add(listHost, 0, 2);
        root.Dock = DockStyle.Fill;
        root.Margin = Padding.Empty;
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 6);
        root.RowCount = 3;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Size = new Size(288, 600);
        root.TabIndex = 0;
        // 
        // header
        // 
        header.ActionText = "⬇ 导出";
        header.Controls.Add(autoHighlightButton);
        header.Controls.Add(batchRemoveButton);
        header.Controls.Add(editButton);
        header.Controls.Add(removeButton);
        header.Dock = DockStyle.Fill;
        header.Icon = "📑";
        header.Margin = Padding.Empty;
        header.Name = "header";
        header.Size = new Size(276, 48);
        header.TabIndex = 0;
        header.TitleText = "高亮";
        // 
        // optionsBar
        // 
        optionsBar.BackColor = Color.FromArgb(247, 248, 251);
        optionsBar.Controls.Add(mergeMultiLineCheck);
        optionsBar.Dock = DockStyle.Fill;
        optionsBar.Margin = new Padding(0, 0, 0, 2);
        optionsBar.Name = "optionsBar";
        optionsBar.Size = new Size(276, 30);
        optionsBar.TabIndex = 1;
        // 
        // mergeMultiLineCheck
        // 
        mergeMultiLineCheck.AutoSize = true;
        mergeMultiLineCheck.BackColor = Color.Transparent;
        mergeMultiLineCheck.Checked = true;
        mergeMultiLineCheck.CheckState = CheckState.Checked;
        mergeMultiLineCheck.FlatStyle = FlatStyle.System;
        mergeMultiLineCheck.ForeColor = Color.FromArgb(15, 23, 42);
        mergeMultiLineCheck.Location = new Point(2, 5);
        mergeMultiLineCheck.Name = "mergeMultiLineCheck";
        mergeMultiLineCheck.Size = new Size(88, 21);
        mergeMultiLineCheck.TabIndex = 0;
        mergeMultiLineCheck.Text = "多行合并";
        mergeMultiLineCheck.UseVisualStyleBackColor = true;
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
        // removeButton
        // 
        removeButton.BackColor = Color.FromArgb(254, 226, 226);
        removeButton.Cursor = Cursors.Hand;
        removeButton.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
        removeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 202, 202);
        removeButton.FlatStyle = FlatStyle.Flat;
        removeButton.Font = new Font("Segoe UI Emoji", 9F);
        removeButton.ForeColor = Color.FromArgb(185, 28, 28);
        removeButton.Margin = Padding.Empty;
        removeButton.Name = "removeButton";
        removeButton.Size = new Size(32, 26);
        removeButton.TabIndex = 1;
        removeButton.Tag = "keep";
        removeButton.Text = "🗑";
        removeButton.UseVisualStyleBackColor = false;
        // 
        // editButton
        // 
        editButton.BackColor = Color.FromArgb(219, 234, 254);
        editButton.Cursor = Cursors.Hand;
        editButton.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);
        editButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(191, 219, 254);
        editButton.FlatStyle = FlatStyle.Flat;
        editButton.Font = new Font("Segoe UI Emoji", 9F);
        editButton.ForeColor = Color.FromArgb(29, 78, 216);
        editButton.Margin = Padding.Empty;
        editButton.Name = "editButton";
        editButton.Size = new Size(32, 26);
        editButton.TabIndex = 2;
        editButton.Tag = "keep";
        editButton.Text = "✏";
        editButton.UseVisualStyleBackColor = false;
        // 
        // batchRemoveButton
        // 
        batchRemoveButton.BackColor = Color.FromArgb(255, 237, 213);
        batchRemoveButton.Cursor = Cursors.Hand;
        batchRemoveButton.FlatAppearance.BorderColor = Color.FromArgb(253, 186, 116);
        batchRemoveButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 215, 170);
        batchRemoveButton.FlatStyle = FlatStyle.Flat;
        batchRemoveButton.Font = new Font("Segoe UI Emoji", 9F);
        batchRemoveButton.ForeColor = Color.FromArgb(194, 65, 12);
        batchRemoveButton.Margin = Padding.Empty;
        batchRemoveButton.Name = "batchRemoveButton";
        batchRemoveButton.Size = new Size(32, 26);
        batchRemoveButton.TabIndex = 3;
        batchRemoveButton.Tag = "keep";
        batchRemoveButton.Text = "🧹";
        batchRemoveButton.UseVisualStyleBackColor = false;
        // 
        // autoHighlightButton
        // 
        autoHighlightButton.BackColor = Color.FromArgb(255, 247, 237);
        autoHighlightButton.Cursor = Cursors.Hand;
        autoHighlightButton.FlatAppearance.BorderColor = Color.FromArgb(253, 186, 116);
        autoHighlightButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 215, 170);
        autoHighlightButton.FlatStyle = FlatStyle.Flat;
        autoHighlightButton.Font = new Font("Segoe UI Emoji", 9F);
        autoHighlightButton.ForeColor = Color.FromArgb(194, 65, 12);
        autoHighlightButton.Margin = Padding.Empty;
        autoHighlightButton.Name = "autoHighlightButton";
        autoHighlightButton.Size = new Size(32, 26);
        autoHighlightButton.TabIndex = 4;
        autoHighlightButton.Tag = "keep";
        autoHighlightButton.Text = "🖍";
        autoHighlightButton.UseVisualStyleBackColor = false;
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
        optionsBar.ResumeLayout(false);
        optionsBar.PerformLayout();
        listHost.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SidebarHeaderBar header;
    private Panel optionsBar;
    private Panel listHost;
    private ListBox highlightList;
    private CheckBox mergeMultiLineCheck;
    private Button removeButton;
    private Button editButton;
    private Button batchRemoveButton;
    private Button autoHighlightButton;
    private ToolTip _tip;
}
