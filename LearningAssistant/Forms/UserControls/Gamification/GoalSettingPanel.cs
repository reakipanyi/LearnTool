using LearningAssistant.Common.UI;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms.UserControls.Gamification
{
    /// <summary>
    /// 目标设置面板
    /// </summary>
    public class GoalSettingPanel : UserControl
    {
        private readonly List<GoalItemPanel> _goalPanels = new();
        private ILearningGoalService? _goalService;
        private string? _currentUserId;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILearningGoalService? GoalService
        {
            get => _goalService;
            set
            {
                _goalService = value;
                LoadGoals();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CurrentUserId
        {
            get => _currentUserId;
            set
            {
                _currentUserId = value;
                LoadGoals();
            }
        }

        public event EventHandler? GoalsChanged;

        public GoalSettingPanel()
        {
            BackColor = Color.Transparent;
            AutoScroll = true;
            Padding = new Padding(15);
        }

        private void LoadGoals()
        {
            if (_goalService == null || string.IsNullOrEmpty(_currentUserId))
                return;

            try
            {
                Controls.Clear();
                _goalPanels.Clear();

                var goals = _goalService.GetGoals(_currentUserId);

                var titleLabel = new Label
                {
                    Text = "🎯 学习目标设置",
                    Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(33, 33, 33),
                    AutoSize = true,
                    Location = new Point(15, 15)
                };
                Controls.Add(titleLabel);

                var descLabel = new Label
                {
                    Text = "设置你的每日学习目标，持续追踪进度",
                    Font = new Font("微软雅黑", 9F),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    AutoSize = true,
                    Location = new Point(15, 45)
                };
                Controls.Add(descLabel);

                int y = 80;
                foreach (var goal in goals)
                {
                    var panel = new GoalItemPanel(goal)
                    {
                        Width = Width - 40,
                        Location = new Point(15, y)
                    };
                    panel.GoalChanged += OnGoalChanged;
                    Controls.Add(panel);
                    _goalPanels.Add(panel);
                    y += panel.Height + 12;
                }

                var streakInfo = _goalService.GetStreakInfo(_currentUserId);
                var streakPanel = CreateStreakPanel(streakInfo);
                streakPanel.Width = Width - 40;
                streakPanel.Location = new Point(15, y + 10);
                Controls.Add(streakPanel);
            }
            catch
            {
            }
        }

        private void OnGoalChanged(object? sender, EventArgs e)
        {
            if (sender is GoalItemPanel panel && _goalService != null && !string.IsNullOrEmpty(_currentUserId))
            {
                _goalService.UpdateGoal(_currentUserId, panel.Goal);
                GoalsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static Panel CreateStreakPanel(StreakInfo streakInfo)
        {
            var panel = new Panel
            {
                Height = 100,
                BackColor = Color.FromArgb(255, 248, 235),
                BorderStyle = BorderStyle.None
            };

            using var path = GdiHelper.CreateRoundedRectPath(
                new Rectangle(0, 0, panel.Width, panel.Height), 12);
            panel.Region = new Region(path);

            var titleLabel = new Label
            {
                Text = "🔥 连续达成统计",
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 126, 34),
                AutoSize = true,
                Location = new Point(15, 12)
            };
            panel.Controls.Add(titleLabel);

            var currentStreakLabel = new Label
            {
                Text = $"当前连续: {streakInfo.CurrentStreak} 天",
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = true,
                Location = new Point(15, 40)
            };
            panel.Controls.Add(currentStreakLabel);

            var longestStreakLabel = new Label
            {
                Text = $"最长连续: {streakInfo.LongestStreak} 天",
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = true,
                Location = new Point(15, 62)
            };
            panel.Controls.Add(longestStreakLabel);

            var rateLabel = new Label
            {
                Text = $"达成率: {streakInfo.CompletionRate:0.#}%",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 125, 50),
                AutoSize = true,
                Location = new Point(150, 40)
            };
            panel.Controls.Add(rateLabel);

            var totalLabel = new Label
            {
                Text = $"总达成: {streakInfo.TotalCompletedDays} 天",
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = true,
                Location = new Point(150, 62)
            };
            panel.Controls.Add(totalLabel);

            return panel;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            foreach (Control control in Controls)
            {
                if (control is GoalItemPanel || (control is Panel p && p.Controls.Count > 0
                    && p.Controls[0] is Label l && l.Text.StartsWith("🔥")))
                {
                    control.Width = Width - 40;
                }
            }
        }
    }

    /// <summary>
    /// 单个目标项面板
    /// </summary>
    public class GoalItemPanel : UserControl
    {
        private readonly CheckBox _enableCheckBox;
        private readonly NumericUpDown _valueNumeric;
        private readonly Label _unitLabel;
        private LearningGoal _goal;

        public LearningGoal Goal => _goal;

        public event EventHandler? GoalChanged;

        public GoalItemPanel(LearningGoal goal)
        {
            _goal = goal;
            Height = 60;
            BackColor = Color.White;

            _enableCheckBox = new CheckBox
            {
                Text = goal.DisplayName,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(15, 10),
                AutoSize = true,
                Checked = goal.Enabled
            };
            _enableCheckBox.CheckedChanged += OnEnableChanged;
            Controls.Add(_enableCheckBox);

            _valueNumeric = new NumericUpDown
            {
                Value = Math.Clamp(goal.TargetValue, 1, 1000),
                Minimum = 1,
                Maximum = 1000,
                Width = 80,
                Location = new Point(15, 30),
                Font = new Font("微软雅黑", 9F),
                Enabled = goal.Enabled
            };
            _valueNumeric.ValueChanged += OnValueChanged;
            Controls.Add(_valueNumeric);

            _unitLabel = new Label
            {
                Text = goal.Unit,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = true,
                Location = new Point(100, 33),
                Enabled = goal.Enabled
            };
            Controls.Add(_unitLabel);

            var descLabel = new Label
            {
                Text = goal.Description,
                Font = new Font("微软雅黑", 8.5F),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoSize = true,
                Location = new Point(150, 33)
            };
            Controls.Add(descLabel);
        }

        private void OnEnableChanged(object? sender, EventArgs e)
        {
            _goal.Enabled = _enableCheckBox.Checked;
            _valueNumeric.Enabled = _enableCheckBox.Checked;
            _unitLabel.Enabled = _enableCheckBox.Checked;
            GoalChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnValueChanged(object? sender, EventArgs e)
        {
            _goal.TargetValue = (int)_valueNumeric.Value;
            GoalChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = GdiHelper.CreateRoundedRectPath(
                new Rectangle(0, 0, Width - 1, Height - 1), 8);
            using var pen = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
            g.DrawPath(pen, path);
        }
    }
}
