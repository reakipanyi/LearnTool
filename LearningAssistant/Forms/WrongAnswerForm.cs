using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    public partial class WrongAnswerForm : Form, IThemeable
    {
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILogger<WrongAnswerForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly string _userId;
        private List<WrongAnswerItem> _currentItems = new();

        public WrongAnswerForm(
            IWrongAnswerService wrongAnswerService,
            ILogger<WrongAnswerForm>? logger = null,
            IThemeService? themeService = null,
            string? userId = null)
        {
            InitializeComponent();
            _wrongAnswerService = wrongAnswerService;
            _logger = logger;
            _themeService = themeService;
            _userId = userId ?? Environment.UserName;

            _themeService?.RegisterThemeable(this);
            LoadWrongAnswers();
        }

        private void LoadWrongAnswers()
        {
            try
            {
                _currentItems = _wrongAnswerService.GetWrongAnswers(_userId);
                UpdateDisplay();
                UpdateStats();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载错题本失败");
                MessageBox.Show($"加载错题本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDisplay()
        {
            listBoxWrongAnswers.Items.Clear();
            foreach (var item in _currentItems)
            {
                string displayText = $"{item.Question} [{item.Subject}]";
                if (item.IsMastered)
                    displayText = "✅ " + displayText;
                else
                    displayText = "❌ " + displayText;
                listBoxWrongAnswers.Items.Add(displayText);
            }
        }

        private void UpdateStats()
        {
            int total = _currentItems.Count;
            int mastered = _currentItems.Count(i => i.IsMastered);
            int unmastered = total - mastered;
            labelStats.Text = $"共 {total} 题 | 待复习 {unmastered} 题 | 已掌握 {mastered} 题";
        }

        private void listBoxWrongAnswers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxWrongAnswers.SelectedIndex < 0) return;

            var item = _currentItems[listBoxWrongAnswers.SelectedIndex];
            textBoxQuestion.Text = item.Question;
            textBoxCorrectAnswer.Text = item.CorrectAnswer;
            textBoxUserAnswer.Text = item.UserAnswer;
            textBoxExplanation.Text = item.Explanation;
            labelDetailStats.Text = $"错误次数: {item.WrongCount} | 复习次数: {item.ReviewCount} | 添加时间: {item.AddedAt:yyyy-MM-dd}";
        }

        private void buttonMarkMastered_Click(object sender, EventArgs e)
        {
            if (listBoxWrongAnswers.SelectedIndex < 0) return;

            var item = _currentItems[listBoxWrongAnswers.SelectedIndex];
            _wrongAnswerService.MarkAsMastered(_userId, item.Id);
            LoadWrongAnswers();
            MessageBox.Show("已标记为已掌握", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (listBoxWrongAnswers.SelectedIndex < 0) return;

            var item = _currentItems[listBoxWrongAnswers.SelectedIndex];
            var result = MessageBox.Show($"确定要删除这道错题吗？\n\n{item.Question}", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _wrongAnswerService.RemoveWrongAnswer(_userId, item.Id);
                LoadWrongAnswers();
            }
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            try
            {
                using var dialog = new SaveFileDialog();
                dialog.Filter = "文本文件|*.txt";
                dialog.FileName = $"错题本_{DateTime.Now:yyyyMMdd}.txt";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _wrongAnswerService.ExportWrongAnswers(_userId, dialog.FileName);
                    MessageBox.Show($"错题本已导出到:\n{dialog.FileName}", "导出成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出错题本失败");
                MessageBox.Show($"导出错题本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void textBoxSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = textBoxSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadWrongAnswers();
                return;
            }

            _currentItems = _currentItems
                .Where(i => i.Question.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || i.Subject.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            UpdateDisplay();
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            ForeColor = colors.TextPrimary;
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private SplitContainer splitContainer1;
        private ListBox listBoxWrongAnswers;
        private TextBox textBoxQuestion;
        private Label labelQuestion;
        private Label labelCorrectAnswer;
        private TextBox textBoxCorrectAnswer;
        private Label labelUserAnswer;
        private TextBox textBoxUserAnswer;
        private Label labelExplanation;
        private TextBox textBoxExplanation;
        private Button buttonMarkMastered;
        private Button buttonDelete;
        private Button buttonExport;
        private Button buttonClose;
        private Label labelStats;
        private TextBox textBoxSearch;
        private Label labelDetailStats;
        private Label labelSearchIcon;

        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            listBoxWrongAnswers = new ListBox();
            textBoxSearch = new TextBox();
            labelSearchIcon = new Label();
            labelStats = new Label();
            textBoxQuestion = new TextBox();
            labelQuestion = new Label();
            labelCorrectAnswer = new Label();
            textBoxCorrectAnswer = new TextBox();
            labelUserAnswer = new Label();
            textBoxUserAnswer = new TextBox();
            labelExplanation = new Label();
            textBoxExplanation = new TextBox();
            buttonMarkMastered = new Button();
            buttonDelete = new Button();
            buttonExport = new Button();
            buttonClose = new Button();
            labelDetailStats = new Label();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(listBoxWrongAnswers);
            splitContainer1.Panel1.Controls.Add(textBoxSearch);
            splitContainer1.Panel1.Controls.Add(labelSearchIcon);
            splitContainer1.Panel1.Controls.Add(labelStats);
            splitContainer1.Panel1MinSize = 200;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(textBoxQuestion);
            splitContainer1.Panel2.Controls.Add(labelQuestion);
            splitContainer1.Panel2.Controls.Add(labelCorrectAnswer);
            splitContainer1.Panel2.Controls.Add(textBoxCorrectAnswer);
            splitContainer1.Panel2.Controls.Add(labelUserAnswer);
            splitContainer1.Panel2.Controls.Add(textBoxUserAnswer);
            splitContainer1.Panel2.Controls.Add(labelExplanation);
            splitContainer1.Panel2.Controls.Add(textBoxExplanation);
            splitContainer1.Panel2.Controls.Add(buttonMarkMastered);
            splitContainer1.Panel2.Controls.Add(buttonDelete);
            splitContainer1.Panel2.Controls.Add(buttonExport);
            splitContainer1.Panel2.Controls.Add(buttonClose);
            splitContainer1.Panel2.Controls.Add(labelDetailStats);
            splitContainer1.Size = new Size(800, 500);
            splitContainer1.SplitterDistance = 280;
            splitContainer1.TabIndex = 0;
            // 
            // listBoxWrongAnswers
            // 
            listBoxWrongAnswers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxWrongAnswers.Font = new Font("微软雅黑", 10F);
            listBoxWrongAnswers.ItemHeight = 25;
            listBoxWrongAnswers.Location = new Point(10, 45);
            listBoxWrongAnswers.Name = "listBoxWrongAnswers";
            listBoxWrongAnswers.Size = new Size(260, 400);
            listBoxWrongAnswers.TabIndex = 0;
            listBoxWrongAnswers.SelectedIndexChanged += listBoxWrongAnswers_SelectedIndexChanged;
            // 
            // textBoxSearch
            // 
            textBoxSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxSearch.BorderStyle = BorderStyle.FixedSingle;
            textBoxSearch.Font = new Font("微软雅黑", 9F);
            textBoxSearch.Location = new Point(30, 10);
            textBoxSearch.Name = "textBoxSearch";
            textBoxSearch.Size = new Size(240, 23);
            textBoxSearch.TabIndex = 1;
            textBoxSearch.TextChanged += textBoxSearch_TextChanged;
            // 
            // labelSearchIcon
            // 
            labelSearchIcon.Font = new Font("Segoe UI Emoji", 10F);
            labelSearchIcon.Location = new Point(10, 10);
            labelSearchIcon.Name = "labelSearchIcon";
            labelSearchIcon.Size = new Size(20, 23);
            labelSearchIcon.TabIndex = 2;
            labelSearchIcon.Text = "🔍";
            labelSearchIcon.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelStats
            // 
            labelStats.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStats.Font = new Font("微软雅黑", 9F);
            labelStats.ForeColor = Color.Gray;
            labelStats.Location = new Point(10, 460);
            labelStats.Name = "labelStats";
            labelStats.Size = new Size(260, 20);
            labelStats.TabIndex = 3;
            labelStats.Text = "共 0 题";
            // 
            // textBoxQuestion
            // 
            textBoxQuestion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxQuestion.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            textBoxQuestion.Location = new Point(10, 30);
            textBoxQuestion.Multiline = true;
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.ReadOnly = true;
            textBoxQuestion.ScrollBars = ScrollBars.Vertical;
            textBoxQuestion.Size = new Size(480, 60);
            textBoxQuestion.TabIndex = 0;
            // 
            // labelQuestion
            // 
            labelQuestion.AutoSize = true;
            labelQuestion.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelQuestion.Location = new Point(10, 10);
            labelQuestion.Name = "labelQuestion";
            labelQuestion.Size = new Size(43, 20);
            labelQuestion.TabIndex = 1;
            labelQuestion.Text = "题目:";
            // 
            // labelCorrectAnswer
            // 
            labelCorrectAnswer.AutoSize = true;
            labelCorrectAnswer.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelCorrectAnswer.ForeColor = Color.Green;
            labelCorrectAnswer.Location = new Point(10, 100);
            labelCorrectAnswer.Name = "labelCorrectAnswer";
            labelCorrectAnswer.Size = new Size(67, 20);
            labelCorrectAnswer.TabIndex = 2;
            labelCorrectAnswer.Text = "正确答案:";
            // 
            // textBoxCorrectAnswer
            // 
            textBoxCorrectAnswer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxCorrectAnswer.Font = new Font("微软雅黑", 10F);
            textBoxCorrectAnswer.ForeColor = Color.Green;
            textBoxCorrectAnswer.Location = new Point(10, 120);
            textBoxCorrectAnswer.Multiline = true;
            textBoxCorrectAnswer.Name = "textBoxCorrectAnswer";
            textBoxCorrectAnswer.ReadOnly = true;
            textBoxCorrectAnswer.ScrollBars = ScrollBars.Vertical;
            textBoxCorrectAnswer.Size = new Size(480, 50);
            textBoxCorrectAnswer.TabIndex = 3;
            // 
            // labelUserAnswer
            // 
            labelUserAnswer.AutoSize = true;
            labelUserAnswer.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelUserAnswer.ForeColor = Color.Red;
            labelUserAnswer.Location = new Point(10, 180);
            labelUserAnswer.Name = "labelUserAnswer";
            labelUserAnswer.Size = new Size(67, 20);
            labelUserAnswer.TabIndex = 4;
            labelUserAnswer.Text = "你的答案:";
            // 
            // textBoxUserAnswer
            // 
            textBoxUserAnswer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxUserAnswer.Font = new Font("微软雅黑", 10F);
            textBoxUserAnswer.ForeColor = Color.Red;
            textBoxUserAnswer.Location = new Point(10, 200);
            textBoxUserAnswer.Multiline = true;
            textBoxUserAnswer.Name = "textBoxUserAnswer";
            textBoxUserAnswer.ReadOnly = true;
            textBoxUserAnswer.ScrollBars = ScrollBars.Vertical;
            textBoxUserAnswer.Size = new Size(480, 50);
            textBoxUserAnswer.TabIndex = 5;
            // 
            // labelExplanation
            // 
            labelExplanation.AutoSize = true;
            labelExplanation.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelExplanation.Location = new Point(10, 260);
            labelExplanation.Name = "labelExplanation";
            labelExplanation.Size = new Size(43, 20);
            labelExplanation.TabIndex = 6;
            labelExplanation.Text = "解析:";
            // 
            // textBoxExplanation
            // 
            textBoxExplanation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxExplanation.Font = new Font("微软雅黑", 10F);
            textBoxExplanation.Location = new Point(10, 280);
            textBoxExplanation.Multiline = true;
            textBoxExplanation.Name = "textBoxExplanation";
            textBoxExplanation.ReadOnly = true;
            textBoxExplanation.ScrollBars = ScrollBars.Vertical;
            textBoxExplanation.Size = new Size(480, 120);
            textBoxExplanation.TabIndex = 7;
            // 
            // buttonMarkMastered
            // 
            buttonMarkMastered.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonMarkMastered.BackColor = Color.FromArgb(76, 175, 80);
            buttonMarkMastered.FlatAppearance.BorderSize = 0;
            buttonMarkMastered.FlatStyle = FlatStyle.Flat;
            buttonMarkMastered.ForeColor = Color.White;
            buttonMarkMastered.Location = new Point(10, 430);
            buttonMarkMastered.Name = "buttonMarkMastered";
            buttonMarkMastered.Size = new Size(100, 30);
            buttonMarkMastered.TabIndex = 8;
            buttonMarkMastered.Text = "✅ 已掌握";
            buttonMarkMastered.UseVisualStyleBackColor = false;
            buttonMarkMastered.Click += buttonMarkMastered_Click;
            // 
            // buttonDelete
            // 
            buttonDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonDelete.BackColor = Color.FromArgb(244, 67, 54);
            buttonDelete.FlatAppearance.BorderSize = 0;
            buttonDelete.FlatStyle = FlatStyle.Flat;
            buttonDelete.ForeColor = Color.White;
            buttonDelete.Location = new Point(120, 430);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(80, 30);
            buttonDelete.TabIndex = 9;
            buttonDelete.Text = "🗑️ 删除";
            buttonDelete.UseVisualStyleBackColor = false;
            buttonDelete.Click += buttonDelete_Click;
            // 
            // buttonExport
            // 
            buttonExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonExport.BackColor = Color.FromArgb(33, 150, 243);
            buttonExport.FlatAppearance.BorderSize = 0;
            buttonExport.FlatStyle = FlatStyle.Flat;
            buttonExport.ForeColor = Color.White;
            buttonExport.Location = new Point(320, 430);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(80, 30);
            buttonExport.TabIndex = 10;
            buttonExport.Text = "📤 导出";
            buttonExport.UseVisualStyleBackColor = false;
            buttonExport.Click += buttonExport_Click;
            // 
            // buttonClose
            // 
            buttonClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClose.BackColor = Color.Gray;
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.FlatStyle = FlatStyle.Flat;
            buttonClose.ForeColor = Color.White;
            buttonClose.Location = new Point(410, 430);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(80, 30);
            buttonClose.TabIndex = 11;
            buttonClose.Text = "关闭";
            buttonClose.UseVisualStyleBackColor = false;
            buttonClose.Click += buttonClose_Click;
            // 
            // labelDetailStats
            // 
            labelDetailStats.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelDetailStats.Font = new Font("微软雅黑", 9F);
            labelDetailStats.ForeColor = Color.Gray;
            labelDetailStats.Location = new Point(10, 405);
            labelDetailStats.Name = "labelDetailStats";
            labelDetailStats.Size = new Size(480, 20);
            labelDetailStats.TabIndex = 12;
            // 
            // WrongAnswerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 500);
            Controls.Add(splitContainer1);
            Name = "WrongAnswerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "📚 错题本";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
