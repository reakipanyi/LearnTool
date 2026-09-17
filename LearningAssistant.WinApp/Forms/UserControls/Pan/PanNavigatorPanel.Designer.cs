namespace LearningAssistant.Forms.UserControls.Pan;

partial class PanNavigatorPanel
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Component Designer generated code
    private void InitializeComponent()
    {
        splitMain = new SplitContainer();
        treeFolders = new TreeView();
        lstFiles = new ListView();
        pnlTop = new Panel();
        cboPath = new ComboBox();
        txtSearch = new TextBox();
        lblPathCaption = new Label();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        pnlTop.SuspendLayout();
        SuspendLayout();
        // 
        // splitMain
        // 
        splitMain.Dock = DockStyle.Fill;
        splitMain.Location = new Point(0, 34);
        splitMain.Name = "splitMain";
        // 
        // splitMain.Panel1
        // 
        splitMain.Panel1.Controls.Add(treeFolders);
        splitMain.Panel1MinSize = 180;
        // 
        // splitMain.Panel2
        // 
        splitMain.Panel2.Controls.Add(lstFiles);
        splitMain.Panel2MinSize = 260;
        splitMain.Size = new Size(600, 466);
        splitMain.SplitterDistance = 200;
        splitMain.SplitterWidth = 6;
        splitMain.TabIndex = 0;
        // 
        // treeFolders
        // 
        treeFolders.AllowDrop = true;
        treeFolders.Dock = DockStyle.Fill;
        treeFolders.Font = new Font("Microsoft YaHei UI", 9F);
        treeFolders.HideSelection = false;
        treeFolders.Location = new Point(0, 0);
        treeFolders.Name = "treeFolders";
        treeFolders.Size = new Size(200, 466);
        treeFolders.TabIndex = 0;
        treeFolders.AfterSelect += treeFolders_AfterSelect;
        treeFolders.DragDrop += treeFolders_DragDrop;
        treeFolders.DragEnter += treeFolders_DragEnter;
        treeFolders.DragOver += treeFolders_DragOver;
        treeFolders.MouseDown += AnyControl_MouseDown;
        // 
        // lstFiles
        // 
        lstFiles.AllowColumnReorder = true;
        lstFiles.AllowDrop = true;
        lstFiles.Dock = DockStyle.Fill;
        lstFiles.Font = new Font("Microsoft YaHei UI", 9F);
        lstFiles.LabelEdit = true;
        lstFiles.Location = new Point(0, 0);
        lstFiles.Name = "lstFiles";
        lstFiles.Size = new Size(394, 466);
        lstFiles.TabIndex = 0;
        lstFiles.UseCompatibleStateImageBehavior = false;
        lstFiles.View = View.Details;
        lstFiles.AfterLabelEdit += lstFiles_AfterLabelEdit;
        lstFiles.BeforeLabelEdit += lstFiles_BeforeLabelEdit;
        lstFiles.ItemDrag += lstFiles_ItemDrag;
        lstFiles.DragDrop += lstFiles_DragDrop;
        lstFiles.DragEnter += lstFiles_DragEnter;
        lstFiles.DragOver += lstFiles_DragOver;
        lstFiles.DoubleClick += lstFiles_DoubleClick;
        lstFiles.KeyDown += lstFiles_KeyDown;
        lstFiles.MouseDown += AnyControl_MouseDown;
        // 
        // pnlTop
        // 
        pnlTop.AllowDrop = true;
        pnlTop.Controls.Add(cboPath);
        pnlTop.Controls.Add(txtSearch);
        pnlTop.Controls.Add(lblPathCaption);
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Location = new Point(0, 0);
        pnlTop.Name = "pnlTop";
        pnlTop.Padding = new Padding(6, 6, 6, 4);
        pnlTop.Size = new Size(600, 34);
        pnlTop.TabIndex = 1;
        pnlTop.DragDrop += lstFiles_DragDrop;
        pnlTop.DragEnter += lstFiles_DragEnter;
        pnlTop.DragOver += lstFiles_DragOver;
        pnlTop.MouseDown += AnyControl_MouseDown;
        // 
        // cboPath
        // 
        cboPath.Font = new Font("Microsoft YaHei UI", 9F);
        cboPath.Location = new Point(52, 5);
        cboPath.Name = "cboPath";
        cboPath.Size = new Size(350, 25);
        cboPath.TabIndex = 1;
        cboPath.SelectedIndexChanged += cboPath_SelectedIndexChanged;
        cboPath.MouseDown += AnyControl_MouseDown;
        // 
        // txtSearch
        // 
        txtSearch.Dock = DockStyle.Right;
        txtSearch.Font = new Font("Microsoft YaHei UI", 9F);
        txtSearch.Location = new Point(408, 6);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new Size(186, 23);
        txtSearch.TabIndex = 2;
        txtSearch.Text = "🔍 搜索文件名（P1 启用）";
        txtSearch.TextChanged += txtSearch_TextChanged;
        txtSearch.MouseDown += AnyControl_MouseDown;
        // 
        // lblPathCaption
        // 
        lblPathCaption.AutoSize = true;
        lblPathCaption.Font = new Font("Microsoft YaHei UI", 9F);
        lblPathCaption.Location = new Point(6, 10);
        lblPathCaption.Name = "lblPathCaption";
        lblPathCaption.Size = new Size(44, 17);
        lblPathCaption.TabIndex = 0;
        lblPathCaption.Text = "路径：";
        lblPathCaption.MouseDown += AnyControl_MouseDown;
        // 
        // PanNavigatorPanel
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(splitMain);
        Controls.Add(pnlTop);
        Font = new Font("Microsoft YaHei UI", 9F);
        Name = "PanNavigatorPanel";
        Size = new Size(600, 500);
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        pnlTop.ResumeLayout(false);
        pnlTop.PerformLayout();
        ResumeLayout(false);
    }
    #endregion

    private System.Windows.Forms.SplitContainer splitMain;
    private System.Windows.Forms.TreeView treeFolders;
    private System.Windows.Forms.ListView lstFiles;
    private System.Windows.Forms.Panel pnlTop;
    private System.Windows.Forms.ComboBox cboPath;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Label lblPathCaption;
}