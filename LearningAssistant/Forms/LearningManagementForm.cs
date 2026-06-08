using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using Microsoft.Extensions.Logging;
using LearningAssistant.Services.Learning;

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
            this.components = new System.ComponentModel.Container();
            this.lblQuote = new System.Windows.Forms.Label();
            this.lblAuthor = new System.Windows.Forms.Label();
            this.lblTodayHeader = new System.Windows.Forms.Label();
            this.lblTodayMinutes = new System.Windows.Forms.Label();
            this.lblTodayItems = new System.Windows.Forms.Label();
            this.lblTodayAccuracy = new System.Windows.Forms.Label();
            this.lblStreak = new System.Windows.Forms.Label();
            this.lblWeekHeader = new System.Windows.Forms.Label();
            this.lblWeekMinutes = new System.Windows.Forms.Label();
            this.lblWeekItems = new System.Windows.Forms.Label();
            this.lblWeekAccuracy = new System.Windows.Forms.Label();
            this.listReminders = new System.Windows.Forms.ListBox();
            this.btnAddReminder = new System.Windows.Forms.Button();
            this.btnGenerateReport = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabOverview = new System.Windows.Forms.TabPage();
            this.tabReminders = new System.Windows.Forms.TabPage();
            this.tabReports = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.tabOverview.SuspendLayout();
            this.tabReminders.SuspendLayout();
            this.SuspendLayout();

            // lblQuote
            this.lblQuote.Location = new System.Drawing.Point(20, 20);
            this.lblQuote.Size = new System.Drawing.Size(500, 60);
            this.lblQuote.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Italic);
            this.lblQuote.ForeColor = System.Drawing.Color.DarkSlateGray;

            // lblAuthor
            this.lblAuthor.Location = new System.Drawing.Point(400, 70);
            this.lblAuthor.Size = new System.Drawing.Size(120, 20);
            this.lblAuthor.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.lblAuthor.ForeColor = System.Drawing.Color.Gray;

            // lblTodayHeader
            this.lblTodayHeader.Text = "今日学习";
            this.lblTodayHeader.Location = new System.Drawing.Point(20, 100);
            this.lblTodayHeader.Size = new System.Drawing.Size(100, 20);
            this.lblTodayHeader.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);

            // lblTodayMinutes
            this.lblTodayMinutes.Location = new System.Drawing.Point(20, 130);
            this.lblTodayMinutes.Size = new System.Drawing.Size(150, 30);
            this.lblTodayMinutes.Font = new System.Drawing.Font("微软雅黑", 14F);

            // lblTodayItems
            this.lblTodayItems.Location = new System.Drawing.Point(180, 130);
            this.lblTodayItems.Size = new System.Drawing.Size(150, 30);
            this.lblTodayItems.Font = new System.Drawing.Font("微软雅黑", 14F);

            // lblTodayAccuracy
            this.lblTodayAccuracy.Location = new System.Drawing.Point(340, 130);
            this.lblTodayAccuracy.Size = new System.Drawing.Size(100, 30);
            this.lblTodayAccuracy.Font = new System.Drawing.Font("微软雅黑", 14F);

            // lblStreak
            this.lblStreak.Location = new System.Drawing.Point(450, 130);
            this.lblStreak.Size = new System.Drawing.Size(100, 30);
            this.lblStreak.Font = new System.Drawing.Font("微软雅黑", 14F);
            this.lblStreak.ForeColor = System.Drawing.Color.OrangeRed;

            // lblWeekHeader
            this.lblWeekHeader.Text = "本周学习";
            this.lblWeekHeader.Location = new System.Drawing.Point(20, 180);
            this.lblWeekHeader.Size = new System.Drawing.Size(100, 20);
            this.lblWeekHeader.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);

            // lblWeekMinutes
            this.lblWeekMinutes.Location = new System.Drawing.Point(20, 210);
            this.lblWeekMinutes.Size = new System.Drawing.Size(150, 30);
            this.lblWeekMinutes.Font = new System.Drawing.Font("微软雅黑", 14F);

            // lblWeekItems
            this.lblWeekItems.Location = new System.Drawing.Point(180, 210);
            this.lblWeekItems.Size = new System.Drawing.Size(150, 30);
            this.lblWeekItems.Font = new System.Drawing.Font("微软雅黑", 14F);

            // lblWeekAccuracy
            this.lblWeekAccuracy.Location = new System.Drawing.Point(340, 210);
            this.lblWeekAccuracy.Size = new System.Drawing.Size(100, 30);
            this.lblWeekAccuracy.Font = new System.Drawing.Font("微软雅黑", 14F);

            // btnRefresh
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.Location = new System.Drawing.Point(520, 10);
            this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);

            // tabControl
            this.tabControl.Controls.Add(this.tabOverview);
            this.tabControl.Controls.Add(this.tabReminders);
            this.tabControl.Controls.Add(this.tabReports);
            this.tabControl.Location = new System.Drawing.Point(10, 10);
            this.tabControl.Size = new System.Drawing.Size(580, 350);

            // tabOverview
            this.tabOverview.Controls.Add(this.lblQuote);
            this.tabOverview.Controls.Add(this.lblAuthor);
            this.tabOverview.Controls.Add(this.lblTodayHeader);
            this.tabOverview.Controls.Add(this.lblTodayMinutes);
            this.tabOverview.Controls.Add(this.lblTodayItems);
            this.tabOverview.Controls.Add(this.lblTodayAccuracy);
            this.tabOverview.Controls.Add(this.lblStreak);
            this.tabOverview.Controls.Add(this.lblWeekHeader);
            this.tabOverview.Controls.Add(this.lblWeekMinutes);
            this.tabOverview.Controls.Add(this.lblWeekItems);
            this.tabOverview.Controls.Add(this.lblWeekAccuracy);
            this.tabOverview.Text = "概览";

            // tabReminders
            this.tabReminders.Controls.Add(this.listReminders);
            this.tabReminders.Controls.Add(this.btnAddReminder);
            this.tabReminders.Text = "提醒管理";

            // listReminders
            this.listReminders.Location = new System.Drawing.Point(20, 20);
            this.listReminders.Size = new System.Drawing.Size(520, 250);

            // btnAddReminder
            this.btnAddReminder.Text = "添加提醒";
            this.btnAddReminder.Location = new System.Drawing.Point(450, 280);
            this.btnAddReminder.Click += new EventHandler(this.btnAddReminder_Click);

            // tabReports
            this.tabReports.Controls.Add(this.btnGenerateReport);
            this.tabReports.Text = "学习报告";

            // btnGenerateReport
            this.btnGenerateReport.Text = "生成今日报告";
            this.btnGenerateReport.Location = new System.Drawing.Point(220, 150);
            this.btnGenerateReport.Click += new EventHandler(this.btnGenerateReport_Click);

            // LearningManagementForm
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnRefresh);
            this.Text = "学习管理";
            this.Load += new EventHandler(this.LearningManagementForm_Load);

            this.tabControl.ResumeLayout(false);
            this.tabOverview.ResumeLayout(false);
            this.tabReminders.ResumeLayout(false);
            this.ResumeLayout(false);
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
