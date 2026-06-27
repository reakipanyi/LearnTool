using LearningAssistant.Services.Learning;
using LearningAssistant.Common.Events;
using LearningAssistant.Services.AI;
using LearningAssistant.Models.AI;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 闪卡复习窗体 - 基于SM-2算法的间隔重复复习
    /// </summary>
    public class FlashcardReviewForm : Form
    {
        #region 控件字段

        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private Label _labelProgress = null!;
        private Panel _panelCard = null!;
        private Label _labelCardContent = null!;
        private Label _labelCardAnswer = null!;
        private Panel _panelButtons = null!;
        private Button _buttonShowAnswer = null!;
        private Button _buttonAgain = null!;
        private Button _buttonHard = null!;
        private Button _buttonGood = null!;
        private Button _buttonEasy = null!;
        private Panel _panelStats = null!;
        private Label _labelDueCount = null!;
        private Label _labelRetention = null!;
        private Panel _panelComplete = null!;
        private Label _labelCompleteTitle = null!;
        private Label _labelCompleteStats = null!;
        private Button _buttonClose = null!;
        private Button _buttonRestart = null!;

        #endregion

        #region 服务和状态

        private readonly ISpacedRepetitionService _spacedRepetitionService;
        private readonly IEventBus? _eventBus;
        private readonly IConversationContextService _conversationService;
        private readonly IUserSessionService _userSessionService;
        private readonly ILogger? _logger;

        private List<ReviewItem> _reviewQueue = new();
        private ReviewItem? _currentItem;
        private int _currentIndex;
        private bool _isAnswerShown;
        private DateTime _cardShownTime;

        #endregion

        #region 常量

        private static readonly Color ColorAgain = Color.FromArgb(244, 67, 54);    // 红色 - 完全不记得
        private static readonly Color ColorHard = Color.FromArgb(255, 152, 0);     // 橙色 - 有点困难
        private static readonly Color ColorGood = Color.FromArgb(76, 175, 80);      // 绿色 - 正常回忆
        private static readonly Color ColorEasy = Color.FromArgb(33, 150, 243);    // 蓝色 - 太简单

        #endregion

        public FlashcardReviewForm(
            ISpacedRepetitionService spacedRepetitionService,
            IConversationContextService conversationService,
            IUserSessionService userSessionService,
            IEventBus? eventBus = null,
            ILogger? logger = null)
        {
            _spacedRepetitionService = spacedRepetitionService ?? throw new ArgumentNullException(nameof(spacedRepetitionService));
            _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));
            _eventBus = eventBus;
            _logger = logger as ILogger;

            InitializeComponent();
            LoadReviewQueue();
        }

        #region 初始化

        private void InitializeComponent()
        {
            Text = "闪卡复习";
            Size = new Size(500, 650);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // 初始化控件
            InitializeHeader();
            InitializeCard();
            InitializeButtons();
            InitializeStats();
            InitializeCompletePanel();

            // 布局
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(0)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Card
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));   // Buttons
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));    // Stats

            mainLayout.Controls.Add(_panelHeader, 0, 0);
            mainLayout.Controls.Add(_panelCard, 0, 1);
            mainLayout.Controls.Add(_panelButtons, 0, 2);
            mainLayout.Controls.Add(_panelStats, 0, 3);

            Controls.Add(mainLayout);
        }

        private void InitializeHeader()
        {
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(33, 150, 243)
            };

            _labelTitle = new Label
            {
                Text = "🧠 闪卡复习",
                Font = new Font("微软雅黑", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            _labelProgress = new Label
            {
                Text = "0 / 0",
                Font = new Font("微软雅黑", 12F),
                ForeColor = Color.FromArgb(220, 235, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(350, 20),
                AutoSize = true
            };

            _panelHeader.Controls.AddRange(new Control[] { _labelTitle, _labelProgress });
        }

        private void InitializeCard()
        {
            _panelCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Margin = new Padding(20, 20, 20, 20),
                Padding = new Padding(20)
            };

            _panelCard.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, _panelCard.Width - 1, _panelCard.Height - 1);
                using var pen = new Pen(Color.FromArgb(220, 220, 230), 1);
                using var brush = new SolidBrush(Color.White);
                e.Graphics.FillRectangle(brush, rect);
                e.Graphics.DrawRectangle(pen, rect);
            };

            _labelCardContent = new Label
            {
                Text = "",
                Font = new Font("微软雅黑", 18F),
                ForeColor = Color.FromArgb(51, 51, 51),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoEllipsis = false
            };

            _labelCardAnswer = new Label
            {
                Text = "",
                Font = new Font("微软雅黑", 14F),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 100,
                Visible = false
            };

            _panelCard.Controls.AddRange(new Control[] { _labelCardContent, _labelCardAnswer });

            _panelCard.Click += (s, e) => ShowAnswer();
        }

        private void InitializeButtons()
        {
            _panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 10)
            };

            var buttonLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };

            for (int i = 0; i < 5; i++)
            {
                buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            }

            // 显示答案按钮
            _buttonShowAnswer = CreateRatingButton("显示答案", Color.FromArgb(100, 100, 100), "空格");
            _buttonShowAnswer.Click += (s, e) => ShowAnswer();

            // 评分按钮
            _buttonAgain = CreateRatingButton("重来\n(1)", ColorAgain, "1");
            _buttonAgain.Click += (s, e) => RateCard(1);

            _buttonHard = CreateRatingButton("困难\n(2)", ColorHard, "2");
            _buttonHard.Click += (s, e) => RateCard(2);

            _buttonGood = CreateRatingButton("正常\n(3)", ColorGood, "3");
            _buttonGood.Click += (s, e) => RateCard(3);

            _buttonEasy = CreateRatingButton("简单\n(4)", ColorEasy, "4");
            _buttonEasy.Click += (s, e) => RateCard(4);

            buttonLayout.Controls.Add(_buttonShowAnswer, 0, 0);
            buttonLayout.Controls.Add(_buttonAgain, 1, 0);
            buttonLayout.Controls.Add(_buttonHard, 2, 0);
            buttonLayout.Controls.Add(_buttonGood, 3, 0);
            buttonLayout.Controls.Add(_buttonEasy, 4, 0);

            _panelButtons.Controls.Add(buttonLayout);

            // 初始状态下只显示"显示答案"按钮
            SetButtonsVisible(false);
        }

        private Button CreateRatingButton(string text, Color backColor, string shortcut)
        {
            return new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10F),
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Cursor = Cursors.Hand,
                Tag = shortcut
            };
        }

        private void InitializeStats()
        {
            _panelStats = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(250, 250, 252),
                Padding = new Padding(20, 10, 20, 10)
            };

            var statsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _labelDueCount = new Label
            {
                Text = "待复习: 0",
                Font = new Font("微软雅黑", 11F),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _labelRetention = new Label
            {
                Text = "保持率: 0%",
                Font = new Font("微软雅黑", 11F),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            statsLayout.Controls.Add(_labelDueCount, 0, 0);
            statsLayout.Controls.Add(_labelRetention, 0, 1);

            _panelStats.Controls.Add(statsLayout);
        }

        private void InitializeCompletePanel()
        {
            _panelComplete = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Visible = false
            };

            _labelCompleteTitle = new Label
            {
                Text = "🎉 复习完成！",
                Font = new Font("微软雅黑", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80
            };

            _labelCompleteStats = new Label
            {
                Text = "",
                Font = new Font("微软雅黑", 12F),
                ForeColor = Color.FromArgb(102, 102, 102),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(60, 10, 60, 10)
            };

            _buttonRestart = new Button
            {
                Text = "再复习一轮",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 11F),
                Size = new Size(140, 40),
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };
            _buttonRestart.Click += (s, e) => RestartReview();

            _buttonClose = new Button
            {
                Text = "关闭",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(102, 102, 102),
                Font = new Font("微软雅黑", 11F),
                Size = new Size(100, 40),
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };
            _buttonClose.Click += (s, e) => Close();

            buttonPanel.Controls.AddRange(new Control[] { _buttonRestart, _buttonClose });

            _panelComplete.Controls.AddRange(new Control[] { buttonPanel, _labelCompleteStats, _labelCompleteTitle });
        }

        #endregion

        #region 核心逻辑

        private void LoadReviewQueue()
        {
            var userId = _userSessionService.CurrentUserId;
            _reviewQueue = _spacedRepetitionService.GetItemsDueForReview(userId);
            _currentIndex = 0;
            _isAnswerShown = false;

            UpdateStats();

            if (_reviewQueue.Count == 0)
            {
                ShowComplete();
            }
            else
            {
                ShowCurrentCard();
            }
        }

        private void ShowCurrentCard()
        {
            if (_currentIndex >= _reviewQueue.Count)
            {
                ShowComplete();
                return;
            }

            _currentItem = _reviewQueue[_currentIndex];
            _isAnswerShown = false;

            _labelCardContent.Text = _currentItem.Content;
            _labelCardAnswer.Text = _currentItem.Answer;
            _labelCardAnswer.Visible = false;

            _labelProgress.Text = $"{_currentIndex + 1} / {_reviewQueue.Count}";

            SetButtonsVisible(false);
            _buttonShowAnswer.Visible = true;

            _cardShownTime = DateTime.Now;

            // 设置学习上下文
            _conversationService.SetLearningContext($"正在复习: {_currentItem.Content}");
        }

        private void ShowAnswer()
        {
            if (_currentItem == null || _isAnswerShown) return;

            _isAnswerShown = true;
            _labelCardAnswer.Visible = true;
            _buttonShowAnswer.Visible = false;

            SetButtonsVisible(true);

            // 动画效果
            AnimateCardFlip();
        }

        private async void RateCard(int rating)
        {
            if (_currentItem == null) return;

            var duration = (int)(DateTime.Now - _cardShownTime).TotalMilliseconds;

            // 调用SM-2算法计算下次复习间隔
            var result = _spacedRepetitionService.CalculateNextReview(_currentItem, rating, duration);

            // 记录日志
            _logger?.LogInformation("闪卡复习评分: 内容={Content}, 评分={Rating}, 下次间隔={Interval}天",
                _currentItem.Content.Length > 20 ? _currentItem.Content.Substring(0, 20) + "..." : _currentItem.Content,
                rating, result.NewInterval);

            // 发布学习事件
            if (rating >= 3)
            {
                _eventBus?.Publish(new ItemLearnedEvent
                {
                    UserId = _currentItem.UserId,
                    ItemContent = _currentItem.Content,
                    LearningType = "Flashcard"
                });
            }
            else
            {
                _eventBus?.Publish(new ItemWrongEvent
                {
                    UserId = _currentItem.UserId,
                    ItemContent = _currentItem.Content,
                    LearningType = "Flashcard"
                });
            }

            // 移动到下一张
            _currentIndex++;
            ShowCurrentCard();

            // 短暂延迟，让用户看到反馈
            await Task.Delay(200);
        }

        private void ShowComplete()
        {
            _panelCard.Visible = false;
            _panelButtons.Visible = false;
            _panelComplete.Visible = true;

            var userId = _userSessionService.CurrentUserId;
            var retention = _spacedRepetitionService.CalculateRetentionRate(userId);
            var todayCount = _spacedRepetitionService.GetTodayReviewCount(userId);

            _labelCompleteStats.Text = $"今日已复习: {todayCount} 张\n记忆保持率: {retention:F1}%";
        }

        private void RestartReview()
        {
            _panelComplete.Visible = false;
            _panelCard.Visible = true;
            _panelButtons.Visible = true;

            LoadReviewQueue();
        }

        private void UpdateStats()
        {
            var userId = _userSessionService.CurrentUserId;
            var retention = _spacedRepetitionService.CalculateRetentionRate(userId);

            _labelDueCount.Text = $"待复习: {_reviewQueue.Count}";
            _labelRetention.Text = $"保持率: {retention:F1}%";
        }

        private void SetButtonsVisible(bool visible)
        {
            _buttonAgain.Visible = visible;
            _buttonHard.Visible = visible;
            _buttonGood.Visible = visible;
            _buttonEasy.Visible = visible;
        }

        private void AnimateCardFlip()
        {
            // 简单的翻转效果
            var originalColor = _panelCard.BackColor;
            _panelCard.BackColor = Color.FromArgb(240, 248, 255);
            Task.Delay(100).ContinueWith(_ =>
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(() => _panelCard.BackColor = originalColor);
                }
            });
        }

        #endregion

        #region 键盘快捷键

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Space:
                    if (!_isAnswerShown)
                        ShowAnswer();
                    return true;

                case Keys.D1:
                case Keys.NumPad1:
                    if (_isAnswerShown) RateCard(1);
                    return true;

                case Keys.D2:
                case Keys.NumPad2:
                    if (_isAnswerShown) RateCard(2);
                    return true;

                case Keys.D3:
                case Keys.NumPad3:
                    if (_isAnswerShown) RateCard(3);
                    return true;

                case Keys.D4:
                case Keys.NumPad4:
                    if (_isAnswerShown) RateCard(4);
                    return true;

                case Keys.Escape:
                    Close();
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion
    }
}
