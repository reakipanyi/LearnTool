using LearningAssistant.Forms.UserControls.Pan;
namespace LearningAssistant.Forms.Pdf;
partial class PanOrganizerForm
{
   private System.ComponentModel.IContainer components = null;
   protected override void Dispose(bool disposing)
   {
       if (disposing && (components != null)) components.Dispose();
       base.Dispose(disposing);
   }
   #region Windows Form Designer generated code
   private void InitializeComponent()
   {
       System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PanOrganizerForm));
       this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
       this.toolStripTop = new System.Windows.Forms.ToolStrip();
       this.btnNavBack = new System.Windows.Forms.ToolStripButton();
       this.btnNavForward = new System.Windows.Forms.ToolStripButton();
       this.toolStripSeparator0 = new System.Windows.Forms.ToolStripSeparator();
       this.btnGoUp = new System.Windows.Forms.ToolStripButton();
       this.btnNewFolder = new System.Windows.Forms.ToolStripButton();
       this.btnRefresh = new System.Windows.Forms.ToolStripButton();
       this.btnPaste = new System.Windows.Forms.ToolStripButton();
       this.btnUndo = new System.Windows.Forms.ToolStripButton();
       this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
       this.btnDryRun = new System.Windows.Forms.ToolStripButton();
       this.btnExecuteTodos = new System.Windows.Forms.ToolStripButton();
       this.btnPullSnapshot = new System.Windows.Forms.ToolStripButton();
       this.btnImportRecs = new System.Windows.Forms.ToolStripButton();
       this.splitMainBottom = new System.Windows.Forms.SplitContainer();
       this.splitContainerMain = new System.Windows.Forms.SplitContainer();
       this.navigatorLeft = new LearningAssistant.Forms.UserControls.Pan.PanNavigatorPanel();
       this.navigatorRight = new LearningAssistant.Forms.UserControls.Pan.PanNavigatorPanel();
       this.tabControlBottom = new System.Windows.Forms.TabControl();
       this.tabTodos = new System.Windows.Forms.TabPage();
       this.tabCompare = new System.Windows.Forms.TabPage();
       this.tabLog = new System.Windows.Forms.TabPage();
       this.txtLog = new System.Windows.Forms.RichTextBox();
       this.statusStripBottom = new System.Windows.Forms.StatusStrip();
       this.lblStatusLeft = new System.Windows.Forms.ToolStripStatusLabel();
       this.sssSpring = new System.Windows.Forms.ToolStripStatusLabel();
       this.lblStatusRight = new System.Windows.Forms.ToolStripStatusLabel();
       this.tableLayoutPanel1.SuspendLayout();
       this.toolStripTop.SuspendLayout();
       ((System.ComponentModel.ISupportInitialize)(this.splitMainBottom)).BeginInit();
       this.splitMainBottom.Panel1.SuspendLayout();
       this.splitMainBottom.Panel2.SuspendLayout();
       this.splitMainBottom.SuspendLayout();
       ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
       this.splitContainerMain.Panel1.SuspendLayout();
       this.splitContainerMain.Panel2.SuspendLayout();
       this.splitContainerMain.SuspendLayout();
       this.tabControlBottom.SuspendLayout();
       this.statusStripBottom.SuspendLayout();
       this.SuspendLayout();
       //
       // tableLayoutPanel1 (3 行：顶 ToolStrip 44 / 中 100% / 底 Tab)
       //
       this.tableLayoutPanel1.ColumnCount = 1;
       this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
       this.tableLayoutPanel1.Controls.Add(this.toolStripTop, 0, 0);
       this.tableLayoutPanel1.Controls.Add(this.splitMainBottom, 0, 1);
       this.tableLayoutPanel1.Controls.Add(this.statusStripBottom, 0, 2);
       this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
       this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
       this.tableLayoutPanel1.Name = "tableLayoutPanel1";
       this.tableLayoutPanel1.RowCount = 3;
       this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
       this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
       this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
       this.tableLayoutPanel1.Size = new System.Drawing.Size(1120, 700);
       this.tableLayoutPanel1.TabIndex = 0;
       //
       // toolStripTop
       //
       this.toolStripTop.Dock = System.Windows.Forms.DockStyle.Fill;
       this.toolStripTop.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.toolStripTop.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
       this.toolStripTop.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
           this.btnNavBack,
           this.btnNavForward,
           this.toolStripSeparator0,
           this.btnGoUp,
           this.btnNewFolder,
           this.btnRefresh,
           this.btnPaste,
           this.btnUndo,
           this.toolStripSeparator1,
           this.btnDryRun,
           this.btnExecuteTodos,
           this.btnPullSnapshot,
           this.btnImportRecs});
       this.toolStripTop.Location = new System.Drawing.Point(0, 0);
       this.toolStripTop.Name = "toolStripTop";
       this.toolStripTop.Padding = new System.Windows.Forms.Padding(8, 6, 1, 6);
       this.toolStripTop.Size = new System.Drawing.Size(1120, 44);
       this.toolStripTop.Stretch = true;
       this.toolStripTop.TabIndex = 0;
       this.toolStripTop.Text = "toolStripTop";
       //
       // toolStripSeparator0
       //
       this.toolStripSeparator0 = new System.Windows.Forms.ToolStripSeparator();
       this.toolStripSeparator0.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
       this.toolStripSeparator0.Name = "toolStripSeparator0";
       //
       // btnNavBack
       //
       this.btnNavBack.AutoSize = false;
       this.btnNavBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
       this.btnNavBack.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnNavBack.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
       this.btnNavBack.Name = "btnNavBack";
       this.btnNavBack.Size = new System.Drawing.Size(32, 26);
       this.btnNavBack.Text = "\u2190";
       this.btnNavBack.ToolTipText = "后退 (Alt+\u2190)";
       this.btnNavBack.Enabled = false;
       this.btnNavBack.Click += new System.EventHandler(this.btnNavBack_Click);
       //
       // btnNavForward
       //
       this.btnNavForward.AutoSize = false;
       this.btnNavForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
       this.btnNavForward.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnNavForward.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnNavForward.Name = "btnNavForward";
       this.btnNavForward.Size = new System.Drawing.Size(32, 26);
       this.btnNavForward.Text = "\u2192";
       this.btnNavForward.ToolTipText = "前进 (Alt+\u2192)";
       this.btnNavForward.Enabled = false;
       this.btnNavForward.Click += new System.EventHandler(this.btnNavForward_Click);
       //
       // btnGoUp
       //
       this.btnGoUp.AutoSize = false;
       this.btnGoUp.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnGoUp.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnGoUp.Name = "btnGoUp";
       this.btnGoUp.Size = new System.Drawing.Size(90, 26);
       this.btnGoUp.Text = "⬆️ 返回上级";
       this.btnGoUp.Click += new System.EventHandler(this.btnGoUp_Click);
       //
       // btnNewFolder
       //
       this.btnNewFolder.AutoSize = false;
       this.btnNewFolder.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnNewFolder.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnNewFolder.Name = "btnNewFolder";
       this.btnNewFolder.Size = new System.Drawing.Size(100, 26);
       this.btnNewFolder.Text = "📁 新建文件夹";
       this.btnNewFolder.Click += new System.EventHandler(this.btnNewFolder_Click);
       //
       // btnRefresh
       //
       this.btnRefresh.AutoSize = false;
       this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnRefresh.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnRefresh.Name = "btnRefresh";
       this.btnRefresh.Size = new System.Drawing.Size(60, 26);
       this.btnRefresh.Text = "🔄 刷新";
       this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
       //
       // btnPaste
       //
       this.btnPaste.AutoSize = false;
       this.btnPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnPaste.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnPaste.Name = "btnPaste";
       this.btnPaste.Size = new System.Drawing.Size(60, 26);
       this.btnPaste.Text = "📋 粘贴";
       this.btnPaste.Click += new System.EventHandler(this.btnPaste_Click);
       //
       // btnUndo
       //
       this.btnUndo.AutoSize = false;
       this.btnUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnUndo.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnUndo.Name = "btnUndo";
       this.btnUndo.Size = new System.Drawing.Size(60, 26);
       this.btnUndo.Text = "↩️ 撤销";
       this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
       //
       // toolStripSeparator1
       //
       this.toolStripSeparator1.Name = "toolStripSeparator1";
       this.toolStripSeparator1.Size = new System.Drawing.Size(6, 32);
       //
       // btnDryRun
       //
       this.btnDryRun.AutoSize = false;
       this.btnDryRun.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnDryRun.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
       this.btnDryRun.Name = "btnDryRun";
       this.btnDryRun.Size = new System.Drawing.Size(100, 26);
       this.btnDryRun.Text = "🧪 Dry-Run";
       this.btnDryRun.ToolTipText = "演练模式：静态校验待办是否会成功（不修改快照、不入撤销栈）";
       this.btnDryRun.Click += new System.EventHandler(this.btnDryRun_Click);
       //
       // btnExecuteTodos
       //
       this.btnExecuteTodos.AutoSize = false;
       this.btnExecuteTodos.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnExecuteTodos.Margin = new System.Windows.Forms.Padding(0, 2, 8, 2);
       this.btnExecuteTodos.Name = "btnExecuteTodos";
       this.btnExecuteTodos.Size = new System.Drawing.Size(120, 26);
       this.btnExecuteTodos.Text = "▶️ 执行选中待办";
       this.btnExecuteTodos.Click += new System.EventHandler(this.btnExecuteTodos_Click);
       //
       // btnPullSnapshot
       //
       this.btnPullSnapshot.AutoSize = false;
       this.btnPullSnapshot.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnPullSnapshot.Margin = new System.Windows.Forms.Padding(0, 2, 8, 2);
       this.btnPullSnapshot.Name = "btnPullSnapshot";
       this.btnPullSnapshot.Size = new System.Drawing.Size(120, 26);
       this.btnPullSnapshot.Text = "📥 拉取快照";
       this.btnPullSnapshot.ToolTipText = "独立入口：输入网盘根路径拉取快照（无需先分析）";
       this.btnPullSnapshot.Click += new System.EventHandler(this.btnPullSnapshot_Click);
       //
       // btnImportRecs
       //
       this.btnImportRecs.AutoSize = false;
       this.btnImportRecs.ImageTransparentColor = System.Drawing.Color.Magenta;
       this.btnImportRecs.Margin = new System.Windows.Forms.Padding(0, 2, 8, 2);
       this.btnImportRecs.Name = "btnImportRecs";
       this.btnImportRecs.Size = new System.Drawing.Size(140, 26);
       this.btnImportRecs.Text = "📤 导入 AI 建议";
       this.btnImportRecs.ToolTipText = "数据流解耦：从剪贴板/JSON 文件导入 AI 生成的 PanRecommendation 建议列表，替代构造函数绑定";
       this.btnImportRecs.Click += new System.EventHandler(this.btnImportRecs_Click);
       //
       // splitMainBottom (上: 双栏浏览器, 下: Tab)
       //
       this.splitMainBottom.Dock = System.Windows.Forms.DockStyle.Fill;
       this.splitMainBottom.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
       this.splitMainBottom.Location = new System.Drawing.Point(3, 47);
       this.splitMainBottom.Name = "splitMainBottom";
       this.splitMainBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
       //
       // splitMainBottom.Panel1 (上部：双栏)
       //
       this.splitMainBottom.Panel1.Controls.Add(this.splitContainerMain);
       this.splitMainBottom.Panel1MinSize = 240;
       //
       // splitMainBottom.Panel2 (下部：Tab)
       //
       this.splitMainBottom.Panel2.Controls.Add(this.tabControlBottom);
       this.splitMainBottom.Panel2MinSize = 120;
       this.splitMainBottom.Size = new System.Drawing.Size(1114, 500);
       this.splitMainBottom.SplitterDistance = 340;
       this.splitMainBottom.SplitterWidth = 6;
       this.splitMainBottom.TabIndex = 1;
       //
       // splitContainerMain (左 navigatorLeft / 右 navigatorRight)
       //
       this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
       this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
       this.splitContainerMain.Name = "splitContainerMain";
       //
       // splitContainerMain.Panel1
       //
       this.splitContainerMain.Panel1.Controls.Add(this.navigatorLeft);
       this.splitContainerMain.Panel1MinSize = 320;
       //
       // splitContainerMain.Panel2
       //
       this.splitContainerMain.Panel2.Controls.Add(this.navigatorRight);
       this.splitContainerMain.Panel2MinSize = 320;
       this.splitContainerMain.Size = new System.Drawing.Size(1114, 340);
       this.splitContainerMain.SplitterDistance = 540;
       this.splitContainerMain.SplitterWidth = 8;
       this.splitContainerMain.TabIndex = 0;
       //
       // navigatorLeft
       //
       this.navigatorLeft.Dock = System.Windows.Forms.DockStyle.Fill;
       this.navigatorLeft.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.navigatorLeft.Location = new System.Drawing.Point(0, 0);
       this.navigatorLeft.Name = "navigatorLeft";
       this.navigatorLeft.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
       this.navigatorLeft.Size = new System.Drawing.Size(540, 340);
       this.navigatorLeft.TabIndex = 0;
       //
       // navigatorRight
       //
       this.navigatorRight.Dock = System.Windows.Forms.DockStyle.Fill;
       this.navigatorRight.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.navigatorRight.Location = new System.Drawing.Point(0, 0);
       this.navigatorRight.Name = "navigatorRight";
       this.navigatorRight.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
       this.navigatorRight.Size = new System.Drawing.Size(566, 340);
       this.navigatorRight.TabIndex = 1;
       //
       // tabControlBottom
       //
       this.tabControlBottom.Controls.Add(this.tabTodos);
       this.tabControlBottom.Controls.Add(this.tabCompare);
       this.tabControlBottom.Controls.Add(this.tabLog);
       this.tabControlBottom.Dock = System.Windows.Forms.DockStyle.Fill;
       this.tabControlBottom.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.tabControlBottom.Location = new System.Drawing.Point(0, 0);
       this.tabControlBottom.Name = "tabControlBottom";
       this.tabControlBottom.SelectedIndex = 0;
       this.tabControlBottom.Size = new System.Drawing.Size(1114, 154);
       this.tabControlBottom.TabIndex = 0;
       //
       // tabTodos
       //
       this.tabTodos.Location = new System.Drawing.Point(4, 26);
       this.tabTodos.Name = "tabTodos";
       this.tabTodos.Padding = new System.Windows.Forms.Padding(3);
       this.tabTodos.Size = new System.Drawing.Size(1106, 124);
       this.tabTodos.TabIndex = 0;
       this.tabTodos.Text = "📋 待办操作";
       this.tabTodos.UseVisualStyleBackColor = true;
       //
       // tabCompare
       //
       this.tabCompare.Location = new System.Drawing.Point(4, 26);
       this.tabCompare.Name = "tabCompare";
       this.tabCompare.Padding = new System.Windows.Forms.Padding(3);
       this.tabCompare.Size = new System.Drawing.Size(1106, 124);
       this.tabCompare.TabIndex = 1;
       this.tabCompare.Text = "🔄 差异对比";
       this.tabCompare.UseVisualStyleBackColor = true;
       //
       // tabLog
       //
       this.tabLog.Location = new System.Drawing.Point(4, 26);
       this.tabLog.Name = "tabLog";
       this.tabLog.Padding = new System.Windows.Forms.Padding(3);
       this.tabLog.Size = new System.Drawing.Size(1106, 124);
       this.tabLog.TabIndex = 1;
       this.tabLog.Text = "📜 执行日志";
       this.tabLog.UseVisualStyleBackColor = true;
       this.tabLog.Controls.Add(this.txtLog);
       //
       // txtLog
       //
       this.txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(247)))), ((int)(((byte)(247)))));
       this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
       this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
       this.txtLog.Location = new System.Drawing.Point(3, 3);
       this.txtLog.Name = "txtLog";
       this.txtLog.ReadOnly = true;
       this.txtLog.Size = new System.Drawing.Size(1100, 118);
       this.txtLog.TabIndex = 0;
       //
       // statusStripBottom
       //
       this.statusStripBottom.BackColor = System.Drawing.SystemColors.Control;
       this.statusStripBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
       this.statusStripBottom.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
           this.lblStatusLeft,
           this.sssSpring,
           this.lblStatusRight});
       this.statusStripBottom.Location = new System.Drawing.Point(0, 674);
       this.statusStripBottom.Name = "statusStripBottom";
       this.statusStripBottom.Size = new System.Drawing.Size(1120, 26);
       this.statusStripBottom.TabIndex = 2;
       this.statusStripBottom.Text = "statusStripBottom";
       //
       // lblStatusLeft
       //
       this.lblStatusLeft.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.lblStatusLeft.Name = "lblStatusLeft";
       this.lblStatusLeft.Size = new System.Drawing.Size(64, 21);
       this.lblStatusLeft.Text = "（未打开）";
       //
       // sssSpring
       //
       this.sssSpring.Name = "sssSpring";
       this.sssSpring.Size = new System.Drawing.Size(980, 21);
       this.sssSpring.Spring = true;
       //
       // lblStatusRight
       //
       this.lblStatusRight.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.lblStatusRight.Name = "lblStatusRight";
       this.lblStatusRight.Size = new System.Drawing.Size(60, 21);
       this.lblStatusRight.Text = "准备就绪";
       //
       // PanOrganizerForm
       //
       this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
       this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
       this.ClientSize = new System.Drawing.Size(1120, 700);
       this.Controls.Add(this.tableLayoutPanel1);
       this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
       this.MinimumSize = new System.Drawing.Size(760, 520);
       this.Name = "PanOrganizerForm";
       this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
       this.Text = "🧰 网盘整理工具（骨架 v0.1）";
       this.tableLayoutPanel1.ResumeLayout(false);
       this.tableLayoutPanel1.PerformLayout();
       this.toolStripTop.ResumeLayout(false);
       this.toolStripTop.PerformLayout();
       this.splitMainBottom.Panel1.ResumeLayout(false);
       this.splitMainBottom.Panel2.ResumeLayout(false);
       ((System.ComponentModel.ISupportInitialize)(this.splitMainBottom)).EndInit();
       this.splitMainBottom.ResumeLayout(false);
       this.splitContainerMain.Panel1.ResumeLayout(false);
       this.splitContainerMain.Panel2.ResumeLayout(false);
       ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
       this.splitContainerMain.ResumeLayout(false);
       this.tabControlBottom.ResumeLayout(false);
       this.statusStripBottom.ResumeLayout(false);
       this.statusStripBottom.PerformLayout();
       this.ResumeLayout(false);
   }
   #endregion
   private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
   private System.Windows.Forms.ToolStrip toolStripTop;
   private System.Windows.Forms.ToolStripButton btnNavBack;
   private System.Windows.Forms.ToolStripButton btnNavForward;
   private System.Windows.Forms.ToolStripSeparator toolStripSeparator0;
   private System.Windows.Forms.ToolStripButton btnGoUp;
   private System.Windows.Forms.ToolStripButton btnNewFolder;
   private System.Windows.Forms.ToolStripButton btnRefresh;
   private System.Windows.Forms.ToolStripButton btnPaste;
   private System.Windows.Forms.ToolStripButton btnUndo;
   private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
   private System.Windows.Forms.ToolStripButton btnDryRun;
   private System.Windows.Forms.ToolStripButton btnExecuteTodos;
   private System.Windows.Forms.ToolStripButton btnPullSnapshot;
   private System.Windows.Forms.ToolStripButton btnImportRecs;
   private System.Windows.Forms.SplitContainer splitMainBottom;
   private System.Windows.Forms.SplitContainer splitContainerMain;
   private PanNavigatorPanel navigatorLeft;
   private PanNavigatorPanel navigatorRight;
   private System.Windows.Forms.TabControl tabControlBottom;
   private System.Windows.Forms.TabPage tabTodos;
   private System.Windows.Forms.TabPage tabCompare;
   private System.Windows.Forms.TabPage tabLog;
   private System.Windows.Forms.RichTextBox txtLog;
   private System.Windows.Forms.StatusStrip statusStripBottom;
   private System.Windows.Forms.ToolStripStatusLabel lblStatusLeft;
   private System.Windows.Forms.ToolStripStatusLabel sssSpring;
   private System.Windows.Forms.ToolStripStatusLabel lblStatusRight;
}