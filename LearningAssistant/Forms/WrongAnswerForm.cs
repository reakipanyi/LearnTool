using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms
{
    public partial class WrongAnswerForm : Form, IThemeable
    {
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILogger<WrongAnswerForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly string _userId;
        private List<WrongAnswerItem> _allItems = new();
        private List<WrongAnswerItem> _filteredItems = new();
        private WrongAnswerItem? _currentItem;
        private string _currentSubject = "全部";
        private string _currentStatus = "全部";
        private bool _isBatchMode = false;
        private HashSet<string> _selectedIds = new();

        private Panel panelSidebar;
        private Label labelSearchTitle;
        private TextBox textBoxSearch;
        private Label labelCategoryTitle;
        private ListBox listBoxCategories;
        private Label labelStatusTitle;
        private RadioButton radioStatusAll;
        private RadioButton radioStatusReview;
        private RadioButton radioStatusMastered;
        private Panel panelBottomActions;
        private Button buttonBatchMode;
        private Button buttonStartReview;
        private Button buttonBatchMastered;
        private Button buttonBatchDelete;
        private Label labelSelectedCount;

        private Panel panelMain;
        private Panel panelStatsBar;
        private Label labelStatTotal;
        private Label labelStatReview;
        private Label labelStatMastered;
        private Label labelStatAccuracy;
        private Label labelStatToday;

        private Panel panelDetail;
        private Label labelQuestion;
        private TextBox textBoxQuestion;
        private Label labelCorrectAnswer;
        private TextBox textBoxCorrectAnswer;
        private Label labelUserAnswer;
        private TextBox textBoxUserAnswer;
        private Label labelExplanation;
        private TextBox textBoxExplanation;
        private Label labelDetailStats;
        private Panel panelDetailButtons;
        private Button buttonMarkMastered;
        private Button buttonDelete;
        private Button buttonExport;
        private Button buttonClose;

        private Form? _reviewForm;

        public WrongAnswerForm(
            IWrongAnswerService wrongAnswerService,
            ILogger<WrongAnswerForm>? logger = null,
            IThemeService? themeService = null,
            string? userId = null)
        {
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _logger = logger;
            _themeService = themeService;
            _userId = userId ?? Environment.UserName;

            InitializeComponent();
            _themeService?.RegisterThemeable(this);
            LoadWrongAnswers();
            LoadCategories();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "📕 错题本";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("微软雅黑", 9F);
            this.MinimumSize = new Size(800, 500);

            SplitContainer splitContainerMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 220,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(230, 230, 240)
            };

            panelSidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            int sidebarY = 12;

            labelSearchTitle = new Label
            {
                Text = "🔍 搜索",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(12, sidebarY),
                AutoSize = true
            };
            sidebarY += 30;

            textBoxSearch = new TextBox
            {
                Location = new Point(12, sidebarY),
                Size = new Size(196, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F),
                PlaceholderText = "搜索题目..."
            };
            textBoxSearch.TextChanged += TextBoxSearch_TextChanged;
            sidebarY += 36;

            labelCategoryTitle = new Label
            {
                Text = "📂 分类筛选",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(12, sidebarY),
                AutoSize = true
            };
            sidebarY += 30;

            listBoxCategories = new ListBox
            {
                Location = new Point(12, sidebarY),
                Size = new Size(196, 150),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("微软雅黑", 10F),
                Cursor = Cursors.Hand
            };
            listBoxCategories.SelectedIndexChanged += ListBoxCategories_SelectedIndexChanged;
            sidebarY += 160;

            labelStatusTitle = new Label
            {
                Text = "🏷️ 状态筛选",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(12, sidebarY),
                AutoSize = true
            };
            sidebarY += 30;

            radioStatusAll = new RadioButton
            {
                Text = "● 全部",
                Location = new Point(16, sidebarY),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(50, 50, 50),
                Checked = true
            };
            radioStatusAll.CheckedChanged += RadioStatus_CheckedChanged;
            sidebarY += 26;

            radioStatusReview = new RadioButton
            {
                Text = "○ 待复习",
                Location = new Point(16, sidebarY),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            radioStatusReview.CheckedChanged += RadioStatus_CheckedChanged;
            sidebarY += 26;

            radioStatusMastered = new RadioButton
            {
                Text = "○ 已掌握",
                Location = new Point(16, sidebarY),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            radioStatusMastered.CheckedChanged += RadioStatus_CheckedChanged;
            sidebarY += 40;

            panelBottomActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(248, 248, 252)
            };

            buttonBatchMode = new Button
            {
                Text = "☑ 批量操作",
                Location = new Point(12, 10),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F)
            };
            buttonBatchMode.FlatAppearance.BorderSize = 0;
            buttonBatchMode.Click += ButtonBatchMode_Click;

            buttonStartReview = new Button
            {
                Text = "📖 开始复习",
                Location = new Point(113, 10),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            buttonStartReview.FlatAppearance.BorderSize = 0;
            buttonStartReview.Click += ButtonStartReview_Click;

            labelSelectedCount = new Label
            {
                Text = "已选择 0 项",
                Location = new Point(12, 15),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(60, 60, 60),
                Visible = false
            };

            buttonBatchMastered = new Button
            {
                Text = "✅ 标记已掌握",
                Location = new Point(100, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Visible = false,
                Enabled = false
            };
            buttonBatchMastered.FlatAppearance.BorderSize = 0;
            buttonBatchMastered.Click += ButtonBatchMastered_Click;

            buttonBatchDelete = new Button
            {
                Text = "🗑️ 批量删除",
                Location = new Point(208, 10),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Visible = false,
                Enabled = false
            };
            buttonBatchDelete.FlatAppearance.BorderSize = 0;
            buttonBatchDelete.Click += ButtonBatchDelete_Click;

            panelBottomActions.Controls.Add(buttonBatchMode);
            panelBottomActions.Controls.Add(buttonStartReview);
            panelBottomActions.Controls.Add(labelSelectedCount);
            panelBottomActions.Controls.Add(buttonBatchMastered);
            panelBottomActions.Controls.Add(buttonBatchDelete);

            panelSidebar.Controls.Add(labelSearchTitle);
            panelSidebar.Controls.Add(textBoxSearch);
            panelSidebar.Controls.Add(labelCategoryTitle);
            panelSidebar.Controls.Add(listBoxCategories);
            panelSidebar.Controls.Add(labelStatusTitle);
            panelSidebar.Controls.Add(radioStatusAll);
            panelSidebar.Controls.Add(radioStatusReview);
            panelSidebar.Controls.Add(radioStatusMastered);
            panelSidebar.Controls.Add(panelBottomActions);

            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 248, 252)
            };

            panelStatsBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };

            labelStatTotal = new Label
            {
                Text = "📊 总错题: 0",
                Location = new Point(20, 18),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33)
            };

            labelStatReview = new Label
            {
                Text = "⏳ 待复习: 0",
                Location = new Point(140, 18),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(244, 67, 54)
            };

            labelStatMastered = new Label
            {
                Text = "✅ 已掌握: 0",
                Location = new Point(260, 18),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(76, 175, 80)
            };

            labelStatAccuracy = new Label
            {
                Text = "🎯 正确率: 0%",
                Location = new Point(380, 18),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(33, 150, 243)
            };

            labelStatToday = new Label
            {
                Text = "📅 今日新增: 0",
                Location = new Point(500, 18),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(156, 39, 176)
            };

            panelStatsBar.Controls.Add(labelStatTotal);
            panelStatsBar.Controls.Add(labelStatReview);
            panelStatsBar.Controls.Add(labelStatMastered);
            panelStatsBar.Controls.Add(labelStatAccuracy);
            panelStatsBar.Controls.Add(labelStatToday);

            SplitContainer splitContainerDetail = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(230, 230, 240)
            };

            Panel panelList = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 248, 252)
            };

            ListBox listBoxWrongAnswers = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 248, 252),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("微软雅黑", 10F),
                ItemHeight = 48,
                Cursor = Cursors.Hand,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            listBoxWrongAnswers.SelectedIndexChanged += ListBoxWrongAnswers_SelectedIndexChanged;
            listBoxWrongAnswers.DrawItem += ListBoxWrongAnswers_DrawItem;
            listBoxWrongAnswers.Name = "listBoxWrongAnswers";

            panelList.Controls.Add(listBoxWrongAnswers);

            panelDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            labelQuestion = new Label
            {
                Text = "❌ 题目:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(20, 15),
                AutoSize = true
            };

            textBoxQuestion = new TextBox
            {
                Location = new Point(20, 40),
                Size = new Size(700, 50),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("微软雅黑", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            labelCorrectAnswer = new Label
            {
                Text = "✅ 正确答案:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(20, 100),
                AutoSize = true
            };

            textBoxCorrectAnswer = new TextBox
            {
                Location = new Point(20, 125),
                Size = new Size(700, 40),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(76, 175, 80),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            labelUserAnswer = new Label
            {
                Text = "❌ 你的答案:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54),
                Location = new Point(20, 175),
                AutoSize = true
            };

            textBoxUserAnswer = new TextBox
            {
                Location = new Point(20, 200),
                Size = new Size(700, 40),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(244, 67, 54),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            labelExplanation = new Label
            {
                Text = "📝 解析:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(20, 250),
                AutoSize = true
            };

            textBoxExplanation = new TextBox
            {
                Location = new Point(20, 275),
                Size = new Size(700, 80),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("微软雅黑", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 252)
            };

            labelDetailStats = new Label
            {
                Text = "",
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(20, 365),
                AutoSize = true
            };

            panelDetailButtons = new Panel
            {
                Location = new Point(20, 395),
                Size = new Size(700, 40),
                BackColor = Color.Transparent
            };

            buttonMarkMastered = new Button
            {
                Text = "✅ 已掌握",
                Location = new Point(0, 0),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonMarkMastered.FlatAppearance.BorderSize = 0;
            buttonMarkMastered.Click += ButtonMarkMastered_Click;

            buttonDelete = new Button
            {
                Text = "🗑️ 删除",
                Location = new Point(108, 0),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonDelete.FlatAppearance.BorderSize = 0;
            buttonDelete.Click += ButtonDelete_Click;

            buttonExport = new Button
            {
                Text = "📤 导出",
                Location = new Point(540, 0),
                Size = new Size(70, 35),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            buttonExport.FlatAppearance.BorderSize = 0;
            buttonExport.Click += ButtonExport_Click;

            buttonClose = new Button
            {
                Text = "关闭",
                Location = new Point(620, 0),
                Size = new Size(80, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F)
            };
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.Click += ButtonClose_Click;

            panelDetailButtons.Controls.Add(buttonMarkMastered);
            panelDetailButtons.Controls.Add(buttonDelete);
            panelDetailButtons.Controls.Add(buttonExport);
            panelDetailButtons.Controls.Add(buttonClose);

            panelDetail.Controls.Add(labelQuestion);
            panelDetail.Controls.Add(textBoxQuestion);
            panelDetail.Controls.Add(labelCorrectAnswer);
            panelDetail.Controls.Add(textBoxCorrectAnswer);
            panelDetail.Controls.Add(labelUserAnswer);
            panelDetail.Controls.Add(textBoxUserAnswer);
            panelDetail.Controls.Add(labelExplanation);
            panelDetail.Controls.Add(textBoxExplanation);
            panelDetail.Controls.Add(labelDetailStats);
            panelDetail.Controls.Add(panelDetailButtons);

            splitContainerDetail.Panel1.Controls.Add(panelList);
            splitContainerDetail.Panel2.Controls.Add(panelDetail);

            panelMain.Controls.Add(splitContainerDetail);
            panelMain.Controls.Add(panelStatsBar);

            splitContainerMain.Panel1.Controls.Add(panelSidebar);
            splitContainerMain.Panel2.Controls.Add(panelMain);

            this.Controls.Add(splitContainerMain);

            splitContainerMain.SplitterDistance = 220;
            splitContainerDetail.SplitterDistance = 200;

            this.listBoxWrongAnswers = listBoxWrongAnswers;

            this.ResumeLayout(false);

            this.Resize += WrongAnswerForm_Resize;
        }

        private void WrongAnswerForm_Resize(object? sender, EventArgs e)
        {
            if (panelDetail == null) return;

            int width = panelDetail.Width - 40;
            if (width < 200) width = 200;

            if (textBoxQuestion != null) textBoxQuestion.Width = width;
            if (textBoxCorrectAnswer != null) textBoxCorrectAnswer.Width = width;
            if (textBoxUserAnswer != null) textBoxUserAnswer.Width = width;
            if (textBoxExplanation != null) textBoxExplanation.Width = width;

            if (panelDetailButtons != null)
            {
                panelDetailButtons.Width = width;
                if (buttonExport != null) buttonExport.Left = width - 158;
                if (buttonClose != null) buttonClose.Left = width - 80;
            }
        }

        private void ListBoxWrongAnswers_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _filteredItems.Count) return;

            var item = _filteredItems[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isChecked = _selectedIds.Contains(item.Id);

            Color bgColor;
            if (isSelected)
                bgColor = Color.FromArgb(230, 230, 250);
            else
                bgColor = e.Index % 2 == 0 ? Color.White : Color.FromArgb(250, 250, 252);

            using var bgBrush = new SolidBrush(bgColor);
            g.FillRectangle(bgBrush, e.Bounds);

            int padding = 10;
            int x = padding;

            if (_isBatchMode)
            {
                int checkSize = 16;
                int checkY = e.Bounds.Y + (e.Bounds.Height - checkSize) / 2;
                using var checkPen = new Pen(Color.FromArgb(150, 150, 150), 1.5f);
                g.DrawRectangle(checkPen, x, checkY, checkSize, checkSize);

                if (isChecked)
                {
                    using var checkBrush = new SolidBrush(Color.FromArgb(63, 81, 181));
                    g.FillRectangle(checkBrush, x + 2, checkY + 2, checkSize - 4, checkSize - 4);

                    using var tickPen = new Pen(Color.White, 2f);
                    g.DrawLine(tickPen, x + 4, checkY + 9, x + 7, checkY + 12);
                    g.DrawLine(tickPen, x + 7, checkY + 12, x + 13, checkY + 5);
                }

                x += checkSize + 10;
            }

            string statusIcon = item.IsMastered ? "✅" : "❌";
            string statusColor = item.IsMastered ? "#4CAF50" : "#F44336";

            using var iconFont = new Font("Segoe UI Emoji", 10F);
            using var iconBrush = new SolidBrush(item.IsMastered ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54));
            g.DrawString(statusIcon, iconFont, iconBrush, x, e.Bounds.Y + padding);

            int titleX = x + 25;
            string title = item.Question.Length > 35 ? item.Question.Substring(0, 35) + "..." : item.Question;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
            var titleRect = new RectangleF(titleX, e.Bounds.Y + padding + 2, e.Bounds.Width - titleX - padding, 20);
            using var titleFormat = new StringFormat
            {
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(title, titleFont, titleBrush, titleRect, titleFormat);

            string meta = $"【{item.Subject}】 错误{item.WrongCount}次 | {item.AddedAt:yyyy-MM-dd}";
            using var metaFont = new Font("微软雅黑", 8F);
            using var metaBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
            var metaRect = new RectangleF(titleX, e.Bounds.Y + padding + 22, e.Bounds.Width - titleX - padding, 16);
            g.DrawString(meta, metaFont, metaBrush, metaRect, titleFormat);
        }

        private void LoadWrongAnswers()
        {
            try
            {
                _allItems = _wrongAnswerService.GetWrongAnswers(_userId);
                _filteredItems = new List<WrongAnswerItem>(_allItems);
                UpdateDisplay();
                UpdateStats();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载错题本失败");
                MessageBox.Show($"加载错题本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                var subjects = _allItems.Select(i => i.Subject).Distinct().OrderBy(s => s).ToList();
                listBoxCategories.Items.Clear();
                listBoxCategories.Items.Add("📋 全部");
                foreach (var sub in subjects)
                {
                    listBoxCategories.Items.Add($"📁 {sub}");
                }
                listBoxCategories.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载分类失败");
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<WrongAnswerItem> query = _allItems;

            if (_currentSubject != "全部")
            {
                query = query.Where(i => i.Subject == _currentSubject);
            }

            if (_currentStatus == "待复习")
            {
                query = query.Where(i => !i.IsMastered);
            }
            else if (_currentStatus == "已掌握")
            {
                query = query.Where(i => i.IsMastered);
            }

            string searchText = textBoxSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(i =>
                    i.Question.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.Subject.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            _filteredItems = query.OrderByDescending(i => i.AddedAt).ToList();
            UpdateDisplay();
            UpdateStats();
        }

        private void UpdateDisplay()
        {
            listBoxWrongAnswers.Items.Clear();
            foreach (var item in _filteredItems)
            {
                listBoxWrongAnswers.Items.Add(item);
            }
        }

        private void UpdateStats()
        {
            int total = _allItems.Count;
            int mastered = _allItems.Count(i => i.IsMastered);
            int unmastered = total - mastered;
            double accuracy = total > 0 ? (double)mastered / total * 100 : 0;
            int todayAdded = _allItems.Count(i => i.AddedAt.Date == DateTime.Today);

            labelStatTotal.Text = $"📊 总错题: {total}";
            labelStatReview.Text = $"⏳ 待复习: {unmastered}";
            labelStatMastered.Text = $"✅ 已掌握: {mastered}";
            labelStatAccuracy.Text = $"🎯 正确率: {accuracy:F1}%";
            labelStatToday.Text = $"📅 今日新增: {todayAdded}";
        }

        private void ListBoxCategories_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxCategories.SelectedIndex < 0) return;

            string selected = listBoxCategories.SelectedItem?.ToString() ?? "";

            if (selected == "📋 全部")
            {
                _currentSubject = "全部";
            }
            else
            {
                _currentSubject = selected.Replace("📁 ", "").Trim();
            }

            ApplyFilters();
        }

        private void RadioStatus_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioStatusAll.Checked)
                _currentStatus = "全部";
            else if (radioStatusReview.Checked)
                _currentStatus = "待复习";
            else if (radioStatusMastered.Checked)
                _currentStatus = "已掌握";

            ApplyFilters();
        }

        private void TextBoxSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ListBoxWrongAnswers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxWrongAnswers.SelectedIndex < 0 || listBoxWrongAnswers.SelectedItem is not WrongAnswerItem item)
            {
                _currentItem = null;
                ClearDetail();
                return;
            }

            if (_isBatchMode)
            {
                if (_selectedIds.Contains(item.Id))
                    _selectedIds.Remove(item.Id);
                else
                    _selectedIds.Add(item.Id);

                listBoxWrongAnswers.Invalidate();
                UpdateSelectedCount();
                return;
            }

            _currentItem = item;
            LoadDetail(item);
            SetDetailButtonsEnabled(true);
        }

        private void LoadDetail(WrongAnswerItem item)
        {
            textBoxQuestion.Text = item.Question;
            textBoxCorrectAnswer.Text = item.CorrectAnswer;
            textBoxUserAnswer.Text = item.UserAnswer;
            textBoxExplanation.Text = item.Explanation;
            labelDetailStats.Text = $"错误次数: {item.WrongCount} | 复习次数: {item.ReviewCount} | 添加时间: {item.AddedAt:yyyy-MM-dd}";
        }

        private void ClearDetail()
        {
            textBoxQuestion.Text = "";
            textBoxCorrectAnswer.Text = "";
            textBoxUserAnswer.Text = "";
            textBoxExplanation.Text = "";
            labelDetailStats.Text = "";
            SetDetailButtonsEnabled(false);
        }

        private void SetDetailButtonsEnabled(bool enabled)
        {
            buttonMarkMastered.Enabled = enabled;
            buttonDelete.Enabled = enabled;
        }

        private void ButtonMarkMastered_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null) return;

            try
            {
                _wrongAnswerService.MarkAsMastered(_userId, _currentItem.Id);
                LoadWrongAnswers();
                LoadCategories();
                MessageBox.Show("已标记为已掌握", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "标记已掌握失败");
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonDelete_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null) return;

            var result = MessageBox.Show($"确定要删除这道错题吗？\n\n{_currentItem.Question}", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                _wrongAnswerService.RemoveWrongAnswer(_userId, _currentItem.Id);
                LoadWrongAnswers();
                LoadCategories();
                ClearDetail();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除错题失败");
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonExport_Click(object? sender, EventArgs e)
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

        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void ButtonBatchMode_Click(object? sender, EventArgs e)
        {
            _isBatchMode = !_isBatchMode;
            _selectedIds.Clear();
            buttonBatchMode.Text = _isBatchMode ? "☑ 退出批量" : "☑ 批量操作";
            buttonBatchMode.BackColor = _isBatchMode ? Color.FromArgb(63, 81, 181) : Color.FromArgb(240, 240, 245);
            buttonBatchMode.ForeColor = _isBatchMode ? Color.White : Color.FromArgb(60, 60, 60);

            buttonStartReview.Visible = !_isBatchMode;
            labelSelectedCount.Visible = _isBatchMode;
            buttonBatchMastered.Visible = _isBatchMode;
            buttonBatchDelete.Visible = _isBatchMode;

            UpdateSelectedCount();
            listBoxWrongAnswers.Invalidate();

            if (_isBatchMode)
            {
                MessageBox.Show("批量操作模式\n\n请点击列表项选择，然后使用底部按钮进行操作",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateSelectedCount()
        {
            labelSelectedCount.Text = $"已选择 {_selectedIds.Count} 项";
            bool hasSelection = _selectedIds.Count > 0;
            buttonBatchMastered.Enabled = hasSelection;
            buttonBatchDelete.Enabled = hasSelection;
        }

        private void ButtonBatchMastered_Click(object? sender, EventArgs e)
        {
            if (_selectedIds.Count == 0) return;

            var result = MessageBox.Show($"确定要将选中的 {_selectedIds.Count} 道错题标记为已掌握吗？",
                "确认批量操作", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                int successCount = 0;
                foreach (var id in _selectedIds)
                {
                    try
                    {
                        _wrongAnswerService.MarkAsMastered(_userId, id);
                        successCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"已成功标记 {successCount} 道错题为已掌握", "操作完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _selectedIds.Clear();
                LoadWrongAnswers();
                LoadCategories();
                UpdateSelectedCount();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量标记已掌握失败");
                MessageBox.Show($"批量操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonBatchDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedIds.Count == 0) return;

            var result = MessageBox.Show($"确定要删除选中的 {_selectedIds.Count} 道错题吗？\n此操作不可恢复！",
                "确认批量删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                int successCount = 0;
                foreach (var id in _selectedIds)
                {
                    try
                    {
                        _wrongAnswerService.RemoveWrongAnswer(_userId, id);
                        successCount++;
                    }
                    catch { }
                }

                MessageBox.Show($"已成功删除 {successCount} 道错题", "操作完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _selectedIds.Clear();
                LoadWrongAnswers();
                LoadCategories();
                UpdateSelectedCount();
                ClearDetail();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量删除失败");
                MessageBox.Show($"批量删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonStartReview_Click(object? sender, EventArgs e)
        {
            var itemsToReview = _filteredItems.Where(i => !i.IsMastered).ToList();
            if (itemsToReview.Count == 0)
            {
                MessageBox.Show("没有需要复习的错题", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StartReviewMode(itemsToReview);
        }

        private void StartReviewMode(List<WrongAnswerItem> items)
        {
            if (items.Count == 0) return;

            int currentIndex = 0;
            bool showAnswer = false;

            Form reviewForm = new Form
            {
                Text = $"📕 错题复习  1/{items.Count}",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(245, 245, 250),
                Font = new Font("微软雅黑", 10F),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Panel panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Label labelSubject = new Label
            {
                Text = $"【{items[0].Subject}】",
                Location = new Point(30, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(63, 81, 181)
            };

            Label labelQuestion = new Label
            {
                Text = items[0].Question,
                Location = new Point(30, 55),
                Size = new Size(440, 100),
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33)
            };

            Label labelUserAnswer = new Label
            {
                Text = $"❌ 你的答案: {items[0].UserAnswer}",
                Location = new Point(30, 165),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(244, 67, 54)
            };

            Button buttonShowAnswer = new Button
            {
                Text = "显示答案 ▼",
                Location = new Point(30, 210),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            buttonShowAnswer.FlatAppearance.BorderSize = 0;

            Panel panelAnswer = new Panel
            {
                Location = new Point(30, 255),
                Size = new Size(440, 40),
                Visible = false
            };

            Label labelCorrect = new Label
            {
                Text = $"✅ 正确答案: {items[0].CorrectAnswer}",
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(0, 0)
            };

            Label labelExplanation = new Label
            {
                Text = $"📝 解析: {items[0].Explanation}",
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(0, 25)
            };

            panelAnswer.Controls.Add(labelCorrect);
            panelAnswer.Controls.Add(labelExplanation);

            Panel panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(248, 248, 252)
            };

            Button buttonRemembered = new Button
            {
                Text = "记住了 ✅",
                Location = new Point(80, 12),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            buttonRemembered.FlatAppearance.BorderSize = 0;

            Button buttonNotYet = new Button
            {
                Text = "还不会 ❌",
                Location = new Point(270, 12),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            buttonNotYet.FlatAppearance.BorderSize = 0;

            panelBottom.Controls.Add(buttonRemembered);
            panelBottom.Controls.Add(buttonNotYet);

            panelContent.Controls.Add(labelSubject);
            panelContent.Controls.Add(labelQuestion);
            panelContent.Controls.Add(labelUserAnswer);
            panelContent.Controls.Add(buttonShowAnswer);
            panelContent.Controls.Add(panelAnswer);

            reviewForm.Controls.Add(panelContent);
            reviewForm.Controls.Add(panelBottom);

            buttonShowAnswer.Click += (s, e) =>
            {
                showAnswer = !showAnswer;
                panelAnswer.Visible = showAnswer;
                buttonShowAnswer.Text = showAnswer ? "收起答案 ▲" : "显示答案 ▼";
                if (showAnswer)
                {
                    panelAnswer.Height = 80;
                }
            };

            void UpdateCard(int idx)
            {
                if (idx < 0 || idx >= items.Count) return;
                reviewForm.Text = $"📕 错题复习  {idx + 1}/{items.Count}";
                labelSubject.Text = $"【{items[idx].Subject}】";
                labelQuestion.Text = items[idx].Question;
                labelUserAnswer.Text = $"❌ 你的答案: {items[idx].UserAnswer}";
                labelCorrect.Text = $"✅ 正确答案: {items[idx].CorrectAnswer}";
                labelExplanation.Text = $"📝 解析: {items[idx].Explanation}";
                showAnswer = false;
                panelAnswer.Visible = false;
                buttonShowAnswer.Text = "显示答案 ▼";
            }

            buttonRemembered.Click += (s, e) =>
            {
                try
                {
                    _wrongAnswerService.MarkAsMastered(_userId, items[currentIndex].Id);
                }
                catch { }

                currentIndex++;
                if (currentIndex >= items.Count)
                {
                    MessageBox.Show("复习完成！太棒了！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reviewForm.Close();
                    LoadWrongAnswers();
                }
                else
                {
                    UpdateCard(currentIndex);
                }
            };

            buttonNotYet.Click += (s, e) =>
            {
                currentIndex++;
                if (currentIndex >= items.Count)
                {
                    MessageBox.Show("复习完成！继续加油！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    reviewForm.Close();
                    LoadWrongAnswers();
                }
                else
                {
                    UpdateCard(currentIndex);
                }
            };

            _reviewForm = reviewForm;
            reviewForm.ShowDialog(this);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (panelSidebar != null)
                panelSidebar.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            if (panelMain != null)
                panelMain.BackColor = colors.Background;
            if (panelStatsBar != null)
                panelStatsBar.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            if (panelDetail != null)
                panelDetail.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            if (panelBottomActions != null)
                panelBottomActions.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(248, 248, 252);

            if (labelSearchTitle != null) labelSearchTitle.ForeColor = colors.TextPrimary;
            if (labelCategoryTitle != null) labelCategoryTitle.ForeColor = colors.TextPrimary;
            if (labelStatusTitle != null) labelStatusTitle.ForeColor = colors.TextPrimary;

            if (textBoxSearch != null)
            {
                textBoxSearch.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.White;
                textBoxSearch.ForeColor = colors.TextPrimary;
            }

            if (listBoxCategories != null)
            {
                listBoxCategories.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
                listBoxCategories.ForeColor = colors.TextPrimary;
            }

            if (radioStatusAll != null) radioStatusAll.ForeColor = colors.TextPrimary;
            if (radioStatusReview != null) radioStatusReview.ForeColor = colors.TextPrimary;
            if (radioStatusMastered != null) radioStatusMastered.ForeColor = colors.TextPrimary;

            if (listBoxWrongAnswers != null)
            {
                listBoxWrongAnswers.BackColor = colors.Background;
                listBoxWrongAnswers.ForeColor = colors.TextPrimary;
            }

            if (labelStatTotal != null) labelStatTotal.ForeColor = colors.TextPrimary;

            if (textBoxQuestion != null)
            {
                textBoxQuestion.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
                textBoxQuestion.ForeColor = colors.TextPrimary;
            }
            if (textBoxCorrectAnswer != null)
            {
                textBoxCorrectAnswer.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
            }
            if (textBoxUserAnswer != null)
            {
                textBoxUserAnswer.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
            }
            if (textBoxExplanation != null)
            {
                textBoxExplanation.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
                textBoxExplanation.ForeColor = colors.TextPrimary;
            }

            if (labelQuestion != null) labelQuestion.ForeColor = colors.TextPrimary;
            if (labelExplanation != null) labelExplanation.ForeColor = colors.TextPrimary;
            if (labelDetailStats != null) labelDetailStats.ForeColor = colors.TextSecondary;
        }

        #region Designer generated fields
        private ListBox listBoxWrongAnswers;
        #endregion
    }
}
