using LearningAssistant.Common.UI;

namespace LearningAssistant.Forms.Pdf;

partial class PdfAudioSidebarPanel
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
        companionPage = new TabPage();
        recordingPage = new TabPage();
        recordingUnavailable = new Label();
        shadowingPage = new TabPage();
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
        root.Size = new Size(555, 1004);
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
        tabs.Controls.Add(companionPage);
        tabs.Controls.Add(recordingPage);
        tabs.Controls.Add(shadowingPage);
        tabs.Dock = DockStyle.Fill;
        tabs.ItemSize = new Size(1, 1);
        tabs.Location = new Point(6, 40);
        tabs.Margin = new Padding(0);
        tabs.Multiline = false;
        tabs.Name = "tabs";
        tabs.SelectedIndex = 0;
        tabs.Size = new Size(543, 960);
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.TabIndex = 1;
        // 
        // companionPage
        // 
        companionPage.BackColor = Color.FromArgb(247, 248, 251);
        companionPage.Location = new Point(4, 4);
        companionPage.Name = "companionPage";
        companionPage.Padding = new Padding(0);
        companionPage.Size = new Size(535, 952);
        companionPage.TabIndex = 0;
        companionPage.UseVisualStyleBackColor = false;
        // 
        // recordingPage
        // 
        recordingPage.BackColor = Color.FromArgb(247, 248, 251);
        recordingPage.Controls.Add(recordingUnavailable);
        recordingPage.Location = new Point(4, 4);
        recordingPage.Name = "recordingPage";
        recordingPage.Padding = new Padding(0);
        recordingPage.Size = new Size(535, 952);
        recordingPage.TabIndex = 1;
        recordingPage.UseVisualStyleBackColor = false;
        // 
        // recordingUnavailable
        // 
        recordingUnavailable.Dock = DockStyle.Fill;
        recordingUnavailable.Font = new Font("Microsoft YaHei UI", 9F);
        recordingUnavailable.ForeColor = Color.Gray;
        recordingUnavailable.Location = new Point(0, 0);
        recordingUnavailable.Name = "recordingUnavailable";
        recordingUnavailable.Size = new Size(535, 952);
        recordingUnavailable.TabIndex = 0;
        recordingUnavailable.Text = "录音服务不可用\r\n请确保 NAudio 库已正确安装";
        recordingUnavailable.TextAlign = ContentAlignment.MiddleCenter;
        recordingUnavailable.Visible = false;
        // 
        // shadowingPage
        // 
        shadowingPage.BackColor = Color.FromArgb(247, 248, 251);
        shadowingPage.Location = new Point(4, 4);
        shadowingPage.Name = "shadowingPage";
        shadowingPage.Padding = new Padding(0);
        shadowingPage.Size = new Size(535, 952);
        shadowingPage.TabIndex = 2;
        shadowingPage.UseVisualStyleBackColor = false;
        // 
        // PdfAudioSidebarPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 251);
        Controls.Add(root);
        Name = "PdfAudioSidebarPanel";
        Size = new Size(555, 1004);
        root.ResumeLayout(false);
        tabs.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel root;
    private SegmentedTabs segments;
    private TabControl tabs;
    private TabPage companionPage;
    private TabPage recordingPage;
    private Label recordingUnavailable;
    private TabPage shadowingPage;
}
