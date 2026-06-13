using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LearningAssistant.Forms
{
    public partial class LearningManagementForm : Form
    {
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly ILearningReminderService _reminderService;
        private readonly LearningReportService _reportService;
        private readonly QuoteService _quoteService;
        private readonly ILogger<LearningManagementForm>? _logger;
        private readonly string _userId;

        public LearningManagementForm(
            ILearningAnalyticsService analyticsService,
            ILearningReminderService reminderService,
            LearningReportService reportService,
            QuoteService quoteService,
            ILogger<LearningManagementForm>? logger = null,
            string? userId = null)
        {
            InitializeComponent();
            _analyticsService = analyticsService;
            _reminderService = reminderService;
            _reportService = reportService;
            _quoteService = quoteService;
            _logger = logger;
            _userId = userId ?? Environment.UserName;

            _logger?.LogInformation("学习管理窗口初始化，用户ID: {UserId}", _userId);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                LoadTodayQuote();
                LoadTodayStats();
                LoadWeeklyStats();
                LoadReminders();
                _logger?.LogDebug("学习数据加载完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载学习数据失败");
                MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadTodayQuote()
        {
            var quote = _quoteService.GetTodayQuote();
            lblQuote.Text = quote.Text;
            lblAuthor.Text = $"—— {quote.Author}";
        }

        private void LoadTodayStats()
        {
            var stats = _analyticsService.GetDailyStatistics(_userId, DateTime.Today);

            lblTodayMinutes.Text = $"{stats.TotalMinutes} 分钟";
            lblTodayItems.Text = $"{stats.TotalItems} 个";
            lblTodayAccuracy.Text = $"{stats.CorrectRate:P2}";
            lblStreak.Text = $"{_analyticsService.GetStudyStreak(_userId)} 天";
        }

        private void LoadWeeklyStats()
        {
            var weekNum = ISOWeek.GetWeekOfYear(DateTime.Today);
            var stats = _analyticsService.GetWeeklyStatistics(_userId, DateTime.Today.Year, weekNum);

            lblWeekMinutes.Text = $"{stats.TotalMinutes} 分钟";
            lblWeekItems.Text = $"{stats.TotalItems} 个";
            lblWeekAccuracy.Text = $"{stats.CorrectRate:P2}";
        }

        private void LoadReminders()
        {
            var reminders = _reminderService.GetUserReminders(_userId);
            listReminders.Items.Clear();

            foreach (var reminder in reminders)
            {
                var status = reminder.Enabled ? "已启用" : "已禁用";
                listReminders.Items.Add($"{reminder.Title} - {reminder.Time:HH:mm} ({status})");
            }
        }

        private void btnAddReminder_Click(object sender, EventArgs e)
        {
            try
            {
                var reminder = new Reminder
                {
                    Id = Guid.NewGuid(),
                    UserId = _userId,
                    Title = "学习提醒",
                    Time = TimeSpan.FromHours(9),
                    RepeatType = ReminderRepeatType.Daily,
                    Enabled = true,
                    CreatedAt = DateTime.Now
                };

                _reminderService.AddReminder(reminder);
                LoadReminders();
                MessageBox.Show("提醒已添加", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logger?.LogInformation("添加提醒: {Title}", reminder.Title);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "添加提醒失败");
                MessageBox.Show($"添加提醒失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                var report = _reportService.GenerateDailyReport(_userId, DateTime.Today);
                var reportText = _reportService.GenerateReportText(report);

                var resultForm = new ResultForm(_logger);
                resultForm.ShowReport(reportText);
                resultForm.Show();
                _logger?.LogInformation("生成学习报告");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成学习报告失败");
                MessageBox.Show($"生成报告失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LearningManagementForm_Load(object sender, EventArgs e)
        {
            _logger?.LogDebug("学习管理窗口加载完成");
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private Label lblQuote;
        private Label lblAuthor;
        private Label lblTodayHeader;
        private Label lblTodayMinutes;
        private Label lblTodayItems;
        private Label lblTodayAccuracy;
        private Label lblStreak;
        private Label lblWeekHeader;
        private Label lblWeekMinutes;
        private Label lblWeekItems;
        private Label lblWeekAccuracy;
        private ListBox listReminders;
        private Button btnAddReminder;
        private Button btnGenerateReport;
        private Button btnRefresh;
        private TabControl tabControl;
        private TabPage tabOverview;
        private TabPage tabReminders;
        private TabPage tabReports;

        private void InitializeComponent()
        {
            lblQuote = new Label();
            lblAuthor = new Label();
            lblTodayHeader = new Label();
            lblTodayMinutes = new Label();
            lblTodayItems = new Label();
            lblTodayAccuracy = new Label();
            lblStreak = new Label();
            lblWeekHeader = new Label();
            lblWeekMinutes = new Label();
            lblWeekItems = new Label();
            lblWeekAccuracy = new Label();
            listReminders = new ListBox();
            btnAddReminder = new Button();
            btnGenerateReport = new Button();
            btnRefresh = new Button();
            tabControl = new TabControl();
            tabOverview = new TabPage();
            tabReminders = new TabPage();
            tabReports = new TabPage();
            tabControl.SuspendLayout();
            tabOverview.SuspendLayout();
            tabReminders.SuspendLayout();
            tabReports.SuspendLayout();
            SuspendLayout();
            // 
            // lblQuote
            // 
            lblQuote.Font = new Font("微软雅黑", 12F, FontStyle.Italic);
            lblQuote.ForeColor = Color.DarkSlateGray;
            lblQuote.Location = new Point(20, 13);
            lblQuote.Name = "lblQuote";
            lblQuote.Size = new Size(530, 87);
            lblQuote.TabIndex = 0;
            // 
            // lblAuthor
            // 
            lblAuthor.Font = new Font("微软雅黑", 10F);
            lblAuthor.ForeColor = Color.Gray;
            lblAuthor.Location = new Point(400, 70);
            lblAuthor.Name = "lblAuthor";
            lblAuthor.Size = new Size(120, 20);
            lblAuthor.TabIndex = 1;
            // 
            // lblTodayHeader
            // 
            lblTodayHeader.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            lblTodayHeader.Location = new Point(20, 130);
            lblTodayHeader.Name = "lblTodayHeader";
            lblTodayHeader.Size = new Size(100, 20);
            lblTodayHeader.TabIndex = 2;
            lblTodayHeader.Text = "今日学习";
            // 
            // lblTodayMinutes
            // 
            lblTodayMinutes.Font = new Font("微软雅黑", 14F);
            lblTodayMinutes.Location = new Point(20, 160);
            lblTodayMinutes.Name = "lblTodayMinutes";
            lblTodayMinutes.Size = new Size(150, 30);
            lblTodayMinutes.TabIndex = 3;
            // 
            // lblTodayItems
            // 
            lblTodayItems.Font = new Font("微软雅黑", 14F);
            lblTodayItems.Location = new Point(180, 160);
            lblTodayItems.Name = "lblTodayItems";
            lblTodayItems.Size = new Size(150, 30);
            lblTodayItems.TabIndex = 4;
            // 
            // lblTodayAccuracy
            // 
            lblTodayAccuracy.Font = new Font("微软雅黑", 14F);
            lblTodayAccuracy.Location = new Point(340, 160);
            lblTodayAccuracy.Name = "lblTodayAccuracy";
            lblTodayAccuracy.Size = new Size(100, 30);
            lblTodayAccuracy.TabIndex = 5;
            // 
            // lblStreak
            // 
            lblStreak.Font = new Font("微软雅黑", 14F);
            lblStreak.ForeColor = Color.OrangeRed;
            lblStreak.Location = new Point(450, 160);
            lblStreak.Name = "lblStreak";
            lblStreak.Size = new Size(100, 30);
            lblStreak.TabIndex = 6;
            // 
            // lblWeekHeader
            // 
            lblWeekHeader.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            lblWeekHeader.Location = new Point(20, 210);
            lblWeekHeader.Name = "lblWeekHeader";
            lblWeekHeader.Size = new Size(100, 20);
            lblWeekHeader.TabIndex = 7;
            lblWeekHeader.Text = "本周学习";
            // 
            // lblWeekMinutes
            // 
            lblWeekMinutes.Font = new Font("微软雅黑", 14F);
            lblWeekMinutes.Location = new Point(20, 240);
            lblWeekMinutes.Name = "lblWeekMinutes";
            lblWeekMinutes.Size = new Size(150, 30);
            lblWeekMinutes.TabIndex = 8;
            // 
            // lblWeekItems
            // 
            lblWeekItems.Font = new Font("微软雅黑", 14F);
            lblWeekItems.Location = new Point(180, 240);
            lblWeekItems.Name = "lblWeekItems";
            lblWeekItems.Size = new Size(150, 30);
            lblWeekItems.TabIndex = 9;
            // 
            // lblWeekAccuracy
            // 
            lblWeekAccuracy.Font = new Font("微软雅黑", 14F);
            lblWeekAccuracy.Location = new Point(340, 240);
            lblWeekAccuracy.Name = "lblWeekAccuracy";
            lblWeekAccuracy.Size = new Size(100, 30);
            lblWeekAccuracy.TabIndex = 10;
            // 
            // listReminders
            // 
            listReminders.Location = new Point(20, 20);
            listReminders.Name = "listReminders";
            listReminders.Size = new Size(520, 242);
            listReminders.TabIndex = 0;
            // 
            // btnAddReminder
            // 
            btnAddReminder.Location = new Point(450, 280);
            btnAddReminder.Name = "btnAddReminder";
            btnAddReminder.Size = new Size(75, 23);
            btnAddReminder.TabIndex = 1;
            btnAddReminder.Text = "添加提醒";
            btnAddReminder.Click += btnAddReminder_Click;
            // 
            // btnGenerateReport
            // 
            btnGenerateReport.Location = new Point(220, 150);
            btnGenerateReport.Name = "btnGenerateReport";
            btnGenerateReport.Size = new Size(75, 23);
            btnGenerateReport.TabIndex = 0;
            btnGenerateReport.Text = "生成今日报告";
            btnGenerateReport.Click += btnGenerateReport_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(520, 10);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(75, 23);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "刷新";
            btnRefresh.Click += btnRefresh_Click;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabOverview);
            tabControl.Controls.Add(tabReminders);
            tabControl.Controls.Add(tabReports);
            tabControl.Location = new Point(10, 10);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(580, 350);
            tabControl.TabIndex = 0;
            // 
            // tabOverview
            // 
            tabOverview.Controls.Add(lblAuthor);
            tabOverview.Controls.Add(lblTodayHeader);
            tabOverview.Controls.Add(lblTodayMinutes);
            tabOverview.Controls.Add(lblTodayItems);
            tabOverview.Controls.Add(lblTodayAccuracy);
            tabOverview.Controls.Add(lblStreak);
            tabOverview.Controls.Add(lblWeekHeader);
            tabOverview.Controls.Add(lblWeekMinutes);
            tabOverview.Controls.Add(lblWeekItems);
            tabOverview.Controls.Add(lblWeekAccuracy);
            tabOverview.Controls.Add(lblQuote);
            tabOverview.Location = new Point(4, 26);
            tabOverview.Name = "tabOverview";
            tabOverview.Size = new Size(572, 320);
            tabOverview.TabIndex = 0;
            tabOverview.Text = "概览";
            // 
            // tabReminders
            // 
            tabReminders.Controls.Add(listReminders);
            tabReminders.Controls.Add(btnAddReminder);
            tabReminders.Location = new Point(4, 26);
            tabReminders.Name = "tabReminders";
            tabReminders.Size = new Size(572, 320);
            tabReminders.TabIndex = 1;
            tabReminders.Text = "提醒管理";
            // 
            // tabReports
            // 
            tabReports.Controls.Add(btnGenerateReport);
            tabReports.Location = new Point(4, 26);
            tabReports.Name = "tabReports";
            tabReports.Size = new Size(572, 320);
            tabReports.TabIndex = 2;
            tabReports.Text = "学习报告";
            // 
            // LearningManagementForm
            // 
            ClientSize = new Size(600, 400);
            Controls.Add(tabControl);
            Controls.Add(btnRefresh);
            Name = "LearningManagementForm";
            Text = "学习管理";
            Load += LearningManagementForm_Load;
            tabControl.ResumeLayout(false);
            tabOverview.ResumeLayout(false);
            tabReminders.ResumeLayout(false);
            tabReports.ResumeLayout(false);
            ResumeLayout(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion 
    }
}
