using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfReadingSidebarPanel
{
#nullable disable

    private System.ComponentModel.IContainer components = null;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        root = new TableLayoutPanel();
        segments = new SegmentedTabs();
        tabs = new TabControl();
        filesPage = new TabPage();
        thumbnailsPage = new TabPage();
        root.SuspendLayout();
        tabs.SuspendLayout();
        SuspendLayout();
        // 
        // root
        // 
        root.BackColor = Color.FromArgb(247, 248, 251);
        root.ColumnCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(segments, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        root.Dock = DockStyle.Fill;
        root.Location = new Point(0, 0);
        root.Margin = new Padding(0);
        root.Name = "root";
        root.Padding = new Padding(6, 2, 6, 4);
        root.RowCount = 2;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Size = new Size(555, 980);
        root.TabIndex = 0;
        // 
        // segments
        // 
        segments.BackColor = Color.FromArgb(247, 248, 251);
        segments.Dock = DockStyle.Fill;
        segments.Location = new Point(6, 2);
        segments.Margin = new Padding(0, 2, 0, 4);
        segments.Name = "segments";
        segments.Size = new Size(543, 32);
        segments.TabIndex = 0;
        // 
        // tabs
        // 
        tabs.Appearance = TabAppearance.FlatButtons;
        tabs.Controls.Add(filesPage);
        tabs.Controls.Add(thumbnailsPage);
        tabs.Dock = DockStyle.Fill;
        tabs.ItemSize = new Size(1, 1);
        tabs.Location = new Point(6, 40);
        tabs.Margin = new Padding(0);
        tabs.Multiline = false;
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(543, 936);
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.TabIndex = 1;
        // 
        // filesPage
        // 
        filesPage.BackColor = Color.FromArgb(247, 248, 251);
        filesPage.Location = new Point(4, 4);
        filesPage.Name = "filesPage";
        filesPage.Padding = new Padding(0);
        filesPage.Size = new Size(535, 928);
        filesPage.TabIndex = 0;
        filesPage.UseVisualStyleBackColor = false;
        // 
        // thumbnailsPage
        // 
        thumbnailsPage.BackColor = Color.FromArgb(247, 248, 251);
        thumbnailsPage.Location = new Point(4, 4);
        thumbnailsPage.Name = "thumbnailsPage";
        thumbnailsPage.Padding = new Padding(0);
        thumbnailsPage.Size = new Size(535, 928);
        thumbnailsPage.TabIndex = 1;
        thumbnailsPage.UseVisualStyleBackColor = false;
        // 
        // PdfReadingSidebarPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfReadingSidebarPanel";
        Size = new Size(555, 980);
        root.ResumeLayout(false);
        tabs.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SegmentedTabs segments;
    private TabControl tabs;
    private TabPage filesPage;
    private TabPage thumbnailsPage;
}
