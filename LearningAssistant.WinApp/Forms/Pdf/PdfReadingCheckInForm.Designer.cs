using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Forms.Pdf
{
    /// <summary>
    /// 打卡表窗体的控件布局：顶部筛选区、指标条、故障横幅、两页表格与底部状态/按钮区。
    /// 控件均为代码构建，便于与现有主题色和字体保持一致。
    /// </summary>
    partial class PdfReadingCheckInForm
    {
        private Panel panelTop;
        private FlowLayoutPanel panelFilterFirst;
        private FlowLayoutPanel panelFilterSecond;
        private Label labelRoot;
        private TextBox textBoxRoot;
        private Button buttonChooseRoot;
        private Button buttonUseCurrent;
        private Label labelMonth;
        private Button buttonPreviousMonth;
        private Label labelMonthValue;
        private Button buttonNextMonth;
        private Button buttonThisMonth;
        private Label labelThreshold;
        private NumericUpDown numericThreshold;
        private Label labelThresholdUnit;
        private Label labelScope;
        private ComboBox comboScope;
        private Label labelDisplayMode;
        private ComboBox comboDisplayMode;
        private Button buttonRefresh;
        private Label labelMetrics;
        private Label labelBanner;
        private TabControl tabMain;
        private TabPage tabSummary;
        private DataGridView gridSummary;
        private Label labelSummaryEmpty;
        private TabPage tabMatrix;
        private DataGridView gridMatrix;
        private Label labelMatrixEmpty;
        private Panel panelBottom;
        private Label labelStatus;
        private Panel panelFooterRight;
        private ProgressBar progressBar;
        private Button buttonClose;
        private ToolTip toolTipMain;

        private void InitializeComponent()
        {
            this.panelTop = new Panel();
            this.panelFilterFirst = new FlowLayoutPanel();
            this.panelFilterSecond = new FlowLayoutPanel();
            this.labelRoot = new Label();
            this.textBoxRoot = new TextBox();
            this.buttonChooseRoot = new Button();
            this.buttonUseCurrent = new Button();
            this.labelMonth = new Label();
            this.buttonPreviousMonth = new Button();
            this.labelMonthValue = new Label();
            this.buttonNextMonth = new Button();
            this.buttonThisMonth = new Button();
            this.labelThreshold = new Label();
            this.numericThreshold = new NumericUpDown();
            this.labelThresholdUnit = new Label();
            this.labelScope = new Label();
            this.comboScope = new ComboBox();
            this.labelDisplayMode = new Label();
            this.comboDisplayMode = new ComboBox();
            this.buttonRefresh = new Button();
            this.labelMetrics = new Label();
            this.labelBanner = new Label();
            this.tabMain = new TabControl();
            this.tabSummary = new TabPage();
            this.gridSummary = new DataGridView();
            this.labelSummaryEmpty = new Label();
            this.tabMatrix = new TabPage();
            this.gridMatrix = new DataGridView();
            this.labelMatrixEmpty = new Label();
            this.panelBottom = new Panel();
            this.labelStatus = new Label();
            this.panelFooterRight = new Panel();
            this.progressBar = new ProgressBar();
            this.buttonClose = new Button();
            this.toolTipMain = new ToolTip();

            // ---- 顶部筛选区：第一行根目录，第二行月份与筛选 ----
            this.panelFilterFirst.Dock = DockStyle.Top;
            this.panelFilterFirst.Height = 38;
            this.panelFilterFirst.FlowDirection = FlowDirection.LeftToRight;
            this.panelFilterFirst.WrapContents = false;
            this.panelFilterFirst.Padding = new Padding(0, 2, 0, 0);

            this.labelRoot.AutoSize = true;
            this.labelRoot.Text = "根目录：";
            this.labelRoot.Margin = new Padding(0, 9, 4, 0);
            this.labelRoot.ForeColor = Color.FromArgb(71, 85, 105);

            this.textBoxRoot.ReadOnly = true;
            this.textBoxRoot.Width = 560;
            this.textBoxRoot.Margin = new Padding(0, 6, 8, 0);
            this.textBoxRoot.BackColor = Color.White;
            this.textBoxRoot.Font = new Font("Microsoft YaHei UI", 9F);

            this.buttonChooseRoot.Text = "选择目录…";
            this.buttonChooseRoot.AutoSize = true;
            this.buttonChooseRoot.MinimumSize = new Size(88, 28);
            this.buttonChooseRoot.Margin = new Padding(0, 3, 6, 0);

            this.buttonUseCurrent.Text = "用当前目录";
            this.buttonUseCurrent.AutoSize = true;
            this.buttonUseCurrent.MinimumSize = new Size(88, 28);
            this.buttonUseCurrent.Margin = new Padding(0, 3, 0, 0);

            this.panelFilterFirst.Controls.Add(this.labelRoot);
            this.panelFilterFirst.Controls.Add(this.textBoxRoot);
            this.panelFilterFirst.Controls.Add(this.buttonChooseRoot);
            this.panelFilterFirst.Controls.Add(this.buttonUseCurrent);

            this.panelFilterSecond.Dock = DockStyle.Top;
            this.panelFilterSecond.Height = 38;
            this.panelFilterSecond.FlowDirection = FlowDirection.LeftToRight;
            this.panelFilterSecond.WrapContents = false;
            this.panelFilterSecond.Padding = new Padding(0, 2, 0, 0);

            this.labelMonth.AutoSize = true;
            this.labelMonth.Text = "月份：";
            this.labelMonth.Margin = new Padding(0, 9, 4, 0);
            this.labelMonth.ForeColor = Color.FromArgb(71, 85, 105);

            this.buttonPreviousMonth.Text = "‹";
            this.buttonPreviousMonth.Width = 30;
            this.buttonPreviousMonth.Height = 28;
            this.buttonPreviousMonth.Margin = new Padding(0, 3, 2, 0);

            this.labelMonthValue.Text = "2026-11";
            this.labelMonthValue.Width = 78;
            this.labelMonthValue.Height = 26;
            this.labelMonthValue.TextAlign = ContentAlignment.MiddleCenter;
            this.labelMonthValue.Margin = new Padding(0, 4, 2, 0);
            this.labelMonthValue.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

            this.buttonNextMonth.Text = "›";
            this.buttonNextMonth.Width = 30;
            this.buttonNextMonth.Height = 28;
            this.buttonNextMonth.Margin = new Padding(0, 3, 4, 0);

            this.buttonThisMonth.Text = "本月";
            this.buttonThisMonth.AutoSize = true;
            this.buttonThisMonth.MinimumSize = new Size(58, 28);
            this.buttonThisMonth.Margin = new Padding(0, 3, 12, 0);

            this.labelThreshold.AutoSize = true;
            this.labelThreshold.Text = "阈值：";
            this.labelThreshold.Margin = new Padding(0, 9, 4, 0);
            this.labelThreshold.ForeColor = Color.FromArgb(71, 85, 105);

            this.numericThreshold.Minimum = 1;
            this.numericThreshold.Maximum = 3600;
            this.numericThreshold.Value = 60;
            this.numericThreshold.Width = 64;
            this.numericThreshold.Margin = new Padding(0, 5, 2, 0);
            this.numericThreshold.TextAlign = HorizontalAlignment.Center;

            this.labelThresholdUnit.AutoSize = true;
            this.labelThresholdUnit.Text = "秒";
            this.labelThresholdUnit.Margin = new Padding(0, 9, 12, 0);
            this.labelThresholdUnit.ForeColor = Color.FromArgb(71, 85, 105);

            this.labelScope.AutoSize = true;
            this.labelScope.Text = "显示范围：";
            this.labelScope.Margin = new Padding(0, 9, 4, 0);
            this.labelScope.ForeColor = Color.FromArgb(71, 85, 105);

            this.comboScope.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboScope.Width = 138;
            this.comboScope.Margin = new Padding(0, 5, 10, 0);
            this.comboScope.Items.AddRange(new object[] { "仅有记录", "全部（含未读）", "全部 + 已失效" });
            this.comboScope.SelectedIndex = 0;

            this.labelDisplayMode.AutoSize = true;
            this.labelDisplayMode.Text = "显示模式：";
            this.labelDisplayMode.Margin = new Padding(0, 9, 4, 0);
            this.labelDisplayMode.ForeColor = Color.FromArgb(71, 85, 105);

            this.comboDisplayMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboDisplayMode.Width = 100;
            this.comboDisplayMode.Margin = new Padding(0, 5, 10, 0);
            this.comboDisplayMode.Items.AddRange(new object[] { "勾选", "分钟数字" });
            this.comboDisplayMode.SelectedIndex = 0;

            this.buttonRefresh.Text = "刷新";
            this.buttonRefresh.AutoSize = true;
            this.buttonRefresh.MinimumSize = new Size(64, 28);
            this.buttonRefresh.Margin = new Padding(0, 3, 0, 0);

            this.panelFilterSecond.Controls.Add(this.labelMonth);
            this.panelFilterSecond.Controls.Add(this.buttonPreviousMonth);
            this.panelFilterSecond.Controls.Add(this.labelMonthValue);
            this.panelFilterSecond.Controls.Add(this.buttonNextMonth);
            this.panelFilterSecond.Controls.Add(this.buttonThisMonth);
            this.panelFilterSecond.Controls.Add(this.labelThreshold);
            this.panelFilterSecond.Controls.Add(this.numericThreshold);
            this.panelFilterSecond.Controls.Add(this.labelThresholdUnit);
            this.panelFilterSecond.Controls.Add(this.labelScope);
            this.panelFilterSecond.Controls.Add(this.comboScope);
            this.panelFilterSecond.Controls.Add(this.labelDisplayMode);
            this.panelFilterSecond.Controls.Add(this.comboDisplayMode);
            this.panelFilterSecond.Controls.Add(this.buttonRefresh);

            this.panelTop.Dock = DockStyle.Top;
            this.panelTop.Height = 88;
            this.panelTop.Padding = new Padding(10, 4, 10, 4);
            // 先加第二行、再加第一行：Dock=Top 时后加入的控件排在上面
            this.panelTop.Controls.Add(this.panelFilterSecond);
            this.panelTop.Controls.Add(this.panelFilterFirst);

            // ---- 指标条与故障横幅 ----
            this.labelMetrics.Dock = DockStyle.Top;
            this.labelMetrics.Height = 32;
            this.labelMetrics.Text = "正在统计…";
            this.labelMetrics.TextAlign = ContentAlignment.MiddleLeft;
            this.labelMetrics.Padding = new Padding(12, 0, 0, 0);
            this.labelMetrics.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            this.labelMetrics.ForeColor = Color.FromArgb(124, 58, 237);

            this.labelBanner.Dock = DockStyle.Top;
            this.labelBanner.Height = 30;
            this.labelBanner.Text = "根目录不可用，请检查磁盘是否连接";
            this.labelBanner.TextAlign = ContentAlignment.MiddleLeft;
            this.labelBanner.Padding = new Padding(12, 0, 0, 0);
            this.labelBanner.BackColor = Color.FromArgb(254, 226, 226);
            this.labelBanner.ForeColor = Color.FromArgb(239, 68, 68);
            this.labelBanner.Visible = false;

            // ---- 两页表格 ----
            this.tabMain.Dock = DockStyle.Fill;
            this.tabMain.Font = new Font("Microsoft YaHei UI", 9F);

            this.tabSummary.Text = "汇总";
            this.tabSummary.Padding = new Padding(3);
            this.tabSummary.UseVisualStyleBackColor = false;

            ConfigureGrid(this.gridSummary);
            this.gridSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.gridSummary.Columns.Add(CreateSummaryColumn("目录 / 文档", 38, 180, DataGridViewContentAlignment.MiddleLeft));
            this.gridSummary.Columns.Add(CreateSummaryColumn("文档数", 7, 50, DataGridViewContentAlignment.MiddleCenter));
            this.gridSummary.Columns.Add(CreateSummaryColumn("已读", 7, 50, DataGridViewContentAlignment.MiddleCenter));
            this.gridSummary.Columns.Add(CreateSummaryColumn("打卡天数", 9, 66, DataGridViewContentAlignment.MiddleCenter));
            this.gridSummary.Columns.Add(CreateSummaryColumn("累计时长", 12, 100, DataGridViewContentAlignment.MiddleCenter));
            this.gridSummary.Columns.Add(CreateSummaryColumn("最近打卡", 9, 76, DataGridViewContentAlignment.MiddleCenter));
            this.gridSummary.Columns.Add(CreateSummaryColumn("连续", 7, 52, DataGridViewContentAlignment.MiddleCenter));
            this.gridSummary.Columns.Add(CreateSummaryColumn("覆盖率", 10, 78, DataGridViewContentAlignment.MiddleLeft));
            this.gridSummary.Columns.Add(CreateSummaryColumn("状态", 10, 82, DataGridViewContentAlignment.MiddleCenter));

            this.labelSummaryEmpty.Dock = DockStyle.Fill;
            this.labelSummaryEmpty.TextAlign = ContentAlignment.MiddleCenter;
            this.labelSummaryEmpty.Font = new Font("Microsoft YaHei UI", 10F);
            this.labelSummaryEmpty.ForeColor = Color.FromArgb(148, 163, 184);
            this.labelSummaryEmpty.Visible = false;

            this.tabSummary.Controls.Add(this.gridSummary);
            this.tabSummary.Controls.Add(this.labelSummaryEmpty);

            this.tabMatrix.Text = "矩阵";
            this.tabMatrix.Padding = new Padding(3);
            this.tabMatrix.UseVisualStyleBackColor = false;

            ConfigureGrid(this.gridMatrix);
            this.gridMatrix.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            this.gridMatrix.ColumnHeadersHeight = 44;
            this.gridMatrix.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridMatrix.Columns.Add(CreateMatrixFixedColumn("目录", 200, DataGridViewContentAlignment.MiddleLeft));
            this.gridMatrix.Columns.Add(CreateMatrixFixedColumn("文档", 210, DataGridViewContentAlignment.MiddleLeft));
            this.gridMatrix.Columns.Add(CreateMatrixFixedColumn("打卡", 54, DataGridViewContentAlignment.MiddleCenter));
            this.gridMatrix.Columns.Add(CreateMatrixFixedColumn("时长", 110, DataGridViewContentAlignment.MiddleCenter));
            this.gridMatrix.Columns.Add(CreateMatrixFixedColumn("连续", 64, DataGridViewContentAlignment.MiddleCenter));

            this.labelMatrixEmpty.Dock = DockStyle.Fill;
            this.labelMatrixEmpty.TextAlign = ContentAlignment.MiddleCenter;
            this.labelMatrixEmpty.Font = new Font("Microsoft YaHei UI", 10F);
            this.labelMatrixEmpty.ForeColor = Color.FromArgb(148, 163, 184);
            this.labelMatrixEmpty.Visible = false;

            this.tabMatrix.Controls.Add(this.gridMatrix);
            this.tabMatrix.Controls.Add(this.labelMatrixEmpty);

            this.tabMain.TabPages.Add(this.tabSummary);
            this.tabMain.TabPages.Add(this.tabMatrix);

            // ---- 底部状态条与按钮 ----
            this.labelStatus.Dock = DockStyle.Fill;
            this.labelStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.labelStatus.Padding = new Padding(4, 0, 0, 0);
            this.labelStatus.ForeColor = Color.FromArgb(100, 116, 139);
            this.labelStatus.Text = "仅统计阅读器前台停留时间 · 图片不参与统计";

            this.progressBar.Style = ProgressBarStyle.Marquee;
            this.progressBar.Width = 120;
            this.progressBar.Height = 16;
            this.progressBar.MarqueeAnimationSpeed = 30;
            this.progressBar.Visible = false;

            this.buttonClose.Text = "关闭";
            this.buttonClose.AutoSize = true;
            this.buttonClose.MinimumSize = new Size(76, 28);

            this.panelFooterRight.Dock = DockStyle.Right;
            this.panelFooterRight.Width = 216;
            this.panelFooterRight.Padding = new Padding(0, 6, 0, 0);
            this.panelFooterRight.Controls.Add(this.progressBar);
            this.panelFooterRight.Controls.Add(this.buttonClose);
            this.progressBar.Location = new Point(0, 6);
            this.buttonClose.Location = new Point(128, 2);

            this.panelBottom.Dock = DockStyle.Bottom;
            this.panelBottom.Height = 44;
            this.panelBottom.Padding = new Padding(8, 4, 8, 4);
            this.panelBottom.Controls.Add(this.labelStatus);
            this.panelBottom.Controls.Add(this.panelFooterRight);

            // ---- 窗体 ----
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.White;
            this.ClientSize = new Size(1240, 780);
            this.DoubleBuffered = true;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(980, 640);
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "阅读打卡表";

            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.labelBanner);
            this.Controls.Add(this.labelMetrics);
            this.Controls.Add(this.panelTop);
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Color.White;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.GridColor = Color.FromArgb(226, 232, 240);
            grid.Font = new Font("Microsoft YaHei UI", 9F);
            grid.RowTemplate.Height = 26;
            grid.Dock = DockStyle.Fill;
        }

        private static DataGridViewTextBoxColumn CreateSummaryColumn(
            string header,
            int fillWeight,
            int minimumWidth,
            DataGridViewContentAlignment alignment)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                FillWeight = fillWeight,
                MinimumWidth = minimumWidth,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Alignment = alignment }
            };
        }

        private static DataGridViewTextBoxColumn CreateMatrixFixedColumn(
            string header,
            int width,
            DataGridViewContentAlignment alignment)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Width = width,
                MinimumWidth = width,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Alignment = alignment }
            };
        }
    }
}
