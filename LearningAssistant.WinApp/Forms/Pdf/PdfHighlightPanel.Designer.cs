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
        listHost = new Panel();
        footer = new FlowLayoutPanel();
        root.SuspendLayout();
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
        colors.Dock = DockStyle.Fill;
        colors.FlowDirection = FlowDirection.LeftToRight;
        colors.Margin = new Padding(0, 2, 0, 2);
        colors.Name = "colors";
        colors.Padding = new Padding(2, 4, 0, 0);
        colors.Size = new Size(276, 32);
        colors.TabIndex = 1;
        colors.WrapContents = false;
        // 
        // listHost
        // 
        listHost.BackColor = Color.White;
        listHost.Dock = DockStyle.Fill;
        listHost.Margin = new Padding(0, 0, 0, 2);
        listHost.Name = "listHost";
        listHost.Size = new Size(276, 464);
        listHost.TabIndex = 2;
        listHost.Tag = "surface";
        // 
        // footer
        // 
        footer.BackColor = Color.FromArgb(247, 248, 251);
        footer.Dock = DockStyle.Fill;
        footer.FlowDirection = FlowDirection.LeftToRight;
        footer.Margin = Padding.Empty;
        footer.Name = "footer";
        footer.Padding = new Padding(0, 4, 0, 0);
        footer.Size = new Size(276, 36);
        footer.TabIndex = 3;
        footer.WrapContents = false;
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
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SidebarHeaderBar header;
    private FlowLayoutPanel colors;
    private Panel listHost;
    private FlowLayoutPanel footer;
}
