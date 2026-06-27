using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Models.Learning;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using CardRecommendation = LearningAssistant.Forms.UserControls.Cards.RecommendationCard;

namespace LearningAssistant.Forms.UserControls.Dashboard
{
    public class DashboardView : UserControl
    {
        private readonly List<StatCard> _statCards = new();
        private readonly List<FeatureCard> _featureCards = new();
        private readonly List<CardRecommendation> _recommendCards = new();
        private Label _labelWelcome = null!;
        private Label _labelSubtitle = null!;
        private Label _labelStatsTitle = null!;
        private Label _labelFeaturesTitle = null!;
        private Panel _panelStats = null!;
        private Panel _panelFeatures = null!;
        private Panel _panelRecommend = null!;
        private Label _labelRecommendTitle = null!;
        private string _userName = "同学";

        public event EventHandler<LearningRecommendation>? RecommendationClicked;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                UpdateWelcomeText();
            }
        }

        public DashboardView()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.FromArgb(245, 245, 250);
            DoubleBuffered = true;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _labelWelcome = new Label
            {
                AutoSize = true,
                Font = new Font("微软雅黑", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Text = $"你好，{_userName}！"
            };

            _labelSubtitle = new Label
            {
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(117, 117, 117),
                Text = "今天也是充满活力的一天，继续加油吧！"
            };

            _labelStatsTitle = new Label
            {
                AutoSize = true,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Text = "📊 今日概览"
            };

            _labelFeaturesTitle = new Label
            {
                AutoSize = true,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Text = "⚡ 快速开始"
            };

            _labelRecommendTitle = new Label
            {
                AutoSize = true,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Text = "🎯 今日推荐"
            };

            _panelStats = new Panel
            {
                BackColor = Color.Transparent
            };

            _panelFeatures = new Panel
            {
                BackColor = Color.Transparent
            };

            _panelRecommend = new Panel
            {
                BackColor = Color.Transparent
            };

            Controls.Add(_labelWelcome);
            Controls.Add(_labelSubtitle);
            Controls.Add(_labelStatsTitle);
            Controls.Add(_panelStats);
            Controls.Add(_labelRecommendTitle);
            Controls.Add(_panelRecommend);
            Controls.Add(_labelFeaturesTitle);
            Controls.Add(_panelFeatures);

            AddStatCard("⏱️", "25分钟", "今日学习", "12%", StatCard.TrendDirection.Up,
                Color.FromArgb(63, 81, 181));
            AddStatCard("🔥", "7天", "连续学习", "3天", StatCard.TrendDirection.Up,
                Color.FromArgb(255, 87, 34));
            AddStatCard("⭐", "120", "总经验值", "+15", StatCard.TrendDirection.Up,
                Color.FromArgb(255, 193, 7));
            AddStatCard("🏆", "Lv.5", "当前等级", "距离Lv.6 30XP", StatCard.TrendDirection.None,
                Color.FromArgb(76, 175, 80));
            AddStatCard("🎯", "0/3", "今日挑战", "待完成", StatCard.TrendDirection.None,
                Color.FromArgb(236, 72, 153));
            AddStatCard("📝", "0", "笔记数", "查看全部", StatCard.TrendDirection.None,
                Color.FromArgb(33, 150, 243));

            // 学习核心
            AddFeatureCard("📚", "开始学习", "打开学习内容", Color.FromArgb(99, 102, 241), Color.FromArgb(139, 92, 246));
            AddFeatureCard("📖", "PDF阅读", "打开PDF阅读器", Color.FromArgb(34, 197, 94), Color.FromArgb(16, 185, 129));

            // 游戏化
            AddFeatureCard("🎯", "每日挑战", "完成每日任务", Color.FromArgb(236, 72, 153), Color.FromArgb(219, 39, 119));
            AddFeatureCard("🏆", "成就徽章", "查看成就", Color.FromArgb(255, 193, 7), Color.FromArgb(251, 146, 60));

            // 工具辅助
            AddFeatureCard("📒", "笔记", "管理学习笔记", Color.FromArgb(156, 39, 176), Color.FromArgb(103, 58, 183));
            AddFeatureCard("📝", "错题本", "复习错题", Color.FromArgb(249, 115, 22), Color.FromArgb(239, 68, 68));

            // 数据分析
            AddFeatureCard("📊", "学习统计", "查看学习数据", Color.FromArgb(14, 165, 233), Color.FromArgb(59, 130, 246));
            AddFeatureCard("⚙️", "设置", "应用设置", Color.FromArgb(108, 117, 125), Color.FromArgb(75, 85, 99));

            InitializeRecommendPanel();
        }

        private void InitializeRecommendPanel()
        {
            var cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _panelRecommend.Controls.Add(cardPanel);
        }

        public void UpdateRecommendations(List<LearningRecommendation> recommendations)
        {
            _recommendCards.Clear();
            _panelRecommend.Controls.Clear();

            if (recommendations == null || recommendations.Count == 0)
            {
                ShowEmptyRecommend();
                return;
            }

            var cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            int y = 5;
            int cardWidth = this.Width - 50;
            if (cardWidth < 300) cardWidth = 300;

            foreach (var rec in recommendations.Take(5))
            {
                var card = new CardRecommendation
                {
                    Title = rec.Title,
                    Reason = rec.Reason,
                    Type = rec.Type,
                    EstimatedMinutes = rec.EstimatedMinutes,
                    Priority = rec.Priority,
                    Location = new Point(25, y),
                    Width = cardWidth,
                    Height = 65
                };
                card.Click += (s, e) => RecommendationClicked?.Invoke(this, rec);
                _recommendCards.Add(card);
                cardPanel.Controls.Add(card);
                y += card.Height + 8;
            }

            _panelRecommend.Controls.Add(cardPanel);
            _panelRecommend.Height = Math.Min(y + 10, 200);
        }

        private void ShowEmptyRecommend()
        {
            var cardPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var recommendLabel = new Label
            {
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(117, 117, 117),
                Text = "暂无推荐内容，添加学习内容后将为你智能推荐",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var iconLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Emoji", 28F),
                Text = "💡"
            };

            cardPanel.Controls.Add(recommendLabel);
            cardPanel.Controls.Add(iconLabel);

            cardPanel.Resize += (s, e) =>
            {
                iconLabel.Location = new Point((cardPanel.Width - 40) / 2, 15);
                recommendLabel.Location = new Point(
                    (cardPanel.Width - (int)recommendLabel.PreferredWidth) / 2,
                    iconLabel.Bottom + 10);
            };

            _panelRecommend.Controls.Add(cardPanel);
        }

        public void AddStatCard(string icon, string value, string label, string trend,
            StatCard.TrendDirection direction, Color accentColor)
        {
            var card = new StatCard
            {
                Icon = icon,
                Value = value,
                Label = label,
                Trend = trend,
                TrendDir = direction,
                AccentColor = accentColor,
                Width = 180,
                Height = 100
            };
            _statCards.Add(card);
            _panelStats.Controls.Add(card);
        }

        public void AddFeatureCard(string icon, string title, string desc,
            Color startColor, Color endColor)
        {
            var card = new FeatureCard
            {
                Icon = icon,
                Title = title,
                Description = "",// desc,
                StartColor = startColor,
                EndColor = endColor,
                Width = 150,
                Height = 130
            };
            _featureCards.Add(card);
            _panelFeatures.Controls.Add(card);
        }

        private void UpdateWelcomeText()
        {
            if (_labelWelcome != null)
            {
                _labelWelcome.Text = $"你好，{_userName}！";
            }
        }

        public void UpdateDashboardStats(
            int todayStudyMinutes,
            int streakDays,
            int totalXP,
            int currentLevel,
            int xpToNextLevel,
            int completedChallenges,
            int totalChallenges,
            int noteCount = 0,
            int todayNewNotes = 0)
        {
            if (_statCards.Count >= 6)
            {
                var studyCard = _statCards[0];
                if (todayStudyMinutes >= 60)
                {
                    int hours = todayStudyMinutes / 60;
                    int mins = todayStudyMinutes % 60;
                    studyCard.Value = hours > 0 && mins > 0 ? $"{hours}时{mins}分" : $"{hours}小时";
                }
                else
                {
                    studyCard.Value = $"{todayStudyMinutes}分钟";
                }
                studyCard.Label = "今日学习";
                studyCard.Trend = todayStudyMinutes > 0 ? $"+{todayStudyMinutes}分" : "开始吧";
                studyCard.TrendDir = todayStudyMinutes > 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;

                var streakCard = _statCards[1];
                streakCard.Value = $"{streakDays}天";
                streakCard.Label = "连续学习";
                streakCard.Trend = streakDays > 0 ? $"已坚持{streakDays}天" : "第1天";
                streakCard.TrendDir = streakDays > 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;

                var xpCard = _statCards[2];
                xpCard.Value = $"{totalXP}";
                xpCard.Label = "总经验值";
                xpCard.Trend = "继续加油";
                xpCard.TrendDir = StatCard.TrendDirection.Up;

                var levelCard = _statCards[3];
                levelCard.Value = $"Lv.{currentLevel}";
                levelCard.Label = "当前等级";
                levelCard.Trend = $"距下一级 {xpToNextLevel}XP";
                levelCard.TrendDir = StatCard.TrendDirection.None;

                var challengeCard = _statCards[4];
                challengeCard.Value = $"{completedChallenges}/{totalChallenges}";
                challengeCard.Label = "今日挑战";
                challengeCard.Trend = completedChallenges >= totalChallenges ? "全部完成" : "进行中";
                challengeCard.TrendDir = completedChallenges >= totalChallenges ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;

                var notesCard = _statCards[5];
                notesCard.Value = $"{noteCount}";
                notesCard.Label = "笔记数";
                notesCard.Trend = todayNewNotes > 0 ? $"今日+{todayNewNotes}" : "查看全部";
                notesCard.TrendDir = todayNewNotes > 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;
            }
        }

        public void UpdateChallengeProgress(int completed, int total)
        {
            if (_statCards.Count >= 5)
            {
                var challengeCard = _statCards[4];
                challengeCard.Value = $"{completed}/{total}";
                challengeCard.Trend = completed >= total ? "已完成" : "进行中";
                challengeCard.TrendDir = completed >= total ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        private void LayoutControls()
        {
            if (_labelWelcome == null || _panelStats == null) return;

            int y = 20;
            int marginLeft = 24;

            _labelWelcome.Location = new Point(marginLeft, y);
            y += _labelWelcome.Height + 4;

            _labelSubtitle.Location = new Point(marginLeft, y);
            y += _labelSubtitle.Height + 20;

            _labelStatsTitle.Location = new Point(marginLeft, y);
            y += _labelStatsTitle.Height + 12;

            _panelStats.Location = new Point(0, y);
            _panelStats.Width = Width;
            _panelStats.Height = 115;
            LayoutStatCards();
            y += _panelStats.Height + 16;

            _labelRecommendTitle.Location = new Point(marginLeft, y);
            y += _labelRecommendTitle.Height + 12;

            _panelRecommend.Location = new Point(0, y);
            _panelRecommend.Width = Width;
            _panelRecommend.Height = 120;
            y += _panelRecommend.Height + 16;

            _labelFeaturesTitle.Location = new Point(marginLeft, y);
            y += _labelFeaturesTitle.Height + 12;

            _panelFeatures.Location = new Point(0, y);
            _panelFeatures.Width = Width;
            _panelFeatures.Height = 150;
            LayoutFeatureCards();
        }

        private void LayoutStatCards()
        {
            int margin = 24;
            int spacing = 16;
            int cardWidth = 180;
            int cardHeight = 100;

            int totalWidth = Width - margin * 2;
            int cardsPerRow = Math.Max(1, (totalWidth + spacing) / (cardWidth + spacing));
            int actualSpacing = (totalWidth - cardsPerRow * cardWidth) / (cardsPerRow + 1);

            for (int i = 0; i < _statCards.Count && i < cardsPerRow; i++)
            {
                var card = _statCards[i];
                card.Location = new Point(
                    margin + actualSpacing + i * (cardWidth + actualSpacing),
                    8);
                card.Size = new Size(cardWidth, cardHeight);
            }
        }

        private void LayoutFeatureCards()
        {
            int margin = 24;
            int spacing = 16;
            int cardWidth = 150;
            int cardHeight = 130;

            int totalWidth = Width - margin * 2;
            int cardsPerRow = Math.Max(1, (totalWidth + spacing) / (cardWidth + spacing));
            int actualSpacing = (totalWidth - cardsPerRow * cardWidth) / (cardsPerRow + 1);

            for (int i = 0; i < _featureCards.Count && i < cardsPerRow; i++)
            {
                var card = _featureCards[i];
                card.Location = new Point(
                    margin + actualSpacing + i * (cardWidth + actualSpacing),
                    10);
                card.Size = new Size(cardWidth, cardHeight);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }
    }
}
