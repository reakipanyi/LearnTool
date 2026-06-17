using LearningAssistant.Common;
using LearningAssistant.Services.Feedback;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 挑战管理器 - 负责每日挑战任务的生成、进度追踪和奖励领取
    /// </summary>
    public class ChallengeManager
    {
        private readonly ILogger<ChallengeManager>? _logger;
        private readonly Action<int>? _onScoreChanged;
        private readonly Action<int>? _onXPChanged;
        private readonly Action? _onLevelUp;
        private readonly Action? _onChallengeCompleted;

        private List<Challenge> _dailyChallenges = new();
        private FlowLayoutPanel? _flowLayoutPanelChallenges;
        private ISoundService? _soundService;

        // 当前学习数据（由外部传入）
        private int _todayLearnedCount = 0;
        private int _quizCorrectCount = 0;
        private int _favoriteCount = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChallengeManager(
            ILogger<ChallengeManager>? logger = null,
            Action<int>? onScoreChanged = null,
            Action<int>? onXPChanged = null,
            Action? onLevelUp = null,
            Action? onChallengeCompleted = null)
        {
            _logger = logger;
            _onScoreChanged = onScoreChanged;
            _onXPChanged = onXPChanged;
            _onLevelUp = onLevelUp;
            _onChallengeCompleted = onChallengeCompleted;
        }

        /// <summary>
        /// 设置UI控件引用
        /// </summary>
        public void SetUI(FlowLayoutPanel flowLayoutPanel, ISoundService? soundService = null)
        {
            _flowLayoutPanelChallenges = flowLayoutPanel;
            _soundService = soundService;
        }

        /// <summary>
        /// 设置当前学习数据
        /// </summary>
        public void SetLearningData(int todayLearnedCount, int quizCorrectCount, int favoriteCount)
        {
            _todayLearnedCount = todayLearnedCount;
            _quizCorrectCount = quizCorrectCount;
            _favoriteCount = favoriteCount;
        }

        /// <summary>
        /// 加载每日挑战
        /// </summary>
        public void Load()
        {
            try
            {
                string challengesPath = Path.Combine(AppPaths.DataDir, "challenges.json");
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                if (File.Exists(challengesPath))
                {
                    string json = File.ReadAllText(challengesPath);
                    var data = JsonSerializer.Deserialize<ChallengeData>(json);

                    if (data?.Date == today)
                    {
                        _dailyChallenges = data.Challenges ?? new List<Challenge>();
                    }
                    else
                    {
                        GenerateDailyChallenges();
                    }
                }
                else
                {
                    GenerateDailyChallenges();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载挑战失败");
                GenerateDailyChallenges();
            }
        }

        /// <summary>
        /// 生成每日挑战
        /// </summary>
        private void GenerateDailyChallenges()
        {
            _dailyChallenges = new List<Challenge>
            {
                new Challenge("daily_learn", "今日学习", "完成今日学习目标", "📚", 10, 10),
                new Challenge("daily_quiz", "答题挑战", "答对10道题目", "🎯", 10, 20),
                new Challenge("daily_streak", "保持连续", "今天至少学习一次", "🔥", 1, 5),
                new Challenge("daily_favorite", "收藏内容", "收藏5个内容", "❤️", 5, 15)
            };
        }

        /// <summary>
        /// 保存挑战数据
        /// </summary>
        public void Save()
        {
            try
            {
                string challengesPath = Path.Combine(AppPaths.DataDir, "challenges.json");
                var challengesDir = Path.GetDirectoryName(challengesPath);
                if (!string.IsNullOrEmpty(challengesDir) && !Directory.Exists(challengesDir))
                    Directory.CreateDirectory(challengesDir);

                var data = new ChallengeData
                {
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    Challenges = _dailyChallenges
                };

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(challengesPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存挑战失败");
            }
        }

        /// <summary>
        /// 更新所有挑战的进度
        /// </summary>
        public void UpdateProgress()
        {
            foreach (var challenge in _dailyChallenges)
            {
                switch (challenge.Id)
                {
                    case "daily_learn":
                        challenge.Current = _todayLearnedCount;
                        break;
                    case "daily_quiz":
                        challenge.Current = _quizCorrectCount;
                        break;
                    case "daily_streak":
                        challenge.Current = _todayLearnedCount > 0 ? 1 : 0;
                        break;
                    case "daily_favorite":
                        challenge.Current = _favoriteCount;
                        break;
                }

                if (challenge.Current >= challenge.Target && !challenge.Completed)
                {
                    challenge.Completed = true;
                    _onChallengeCompleted?.Invoke();
                }
            }

            Save();
            UpdateDisplay();
        }

        /// <summary>
        /// 领取挑战奖励
        /// </summary>
        public void ClaimReward(Challenge challenge)
        {
            if (challenge.Claimed) return;

            challenge.Claimed = true;
            _onScoreChanged?.Invoke(challenge.Reward);
            _onXPChanged?.Invoke(challenge.Reward);
            _onLevelUp?.Invoke();
            _soundService?.PlaySuccess();

            Save();
            UpdateDisplay();
        }

        /// <summary>
        /// 更新挑战显示
        /// </summary>
        public void UpdateDisplay()
        {
            if (_flowLayoutPanelChallenges == null) return;

            _flowLayoutPanelChallenges.Controls.Clear();

            foreach (var challenge in _dailyChallenges)
            {
                Panel panel = new Panel
                {
                    Size = new Size(150, 70),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = SystemColors.Control
                };

                Label labelName = new Label
                {
                    Text = challenge.Name,
                    Location = new Point(5, 5),
                    Size = new Size(140, 15),
                    Font = new Font("微软雅黑", 9F)
                };

                Label labelProgress = new Label
                {
                    Text = $"{challenge.Current}/{challenge.Target}",
                    Location = new Point(5, 25),
                    Size = new Size(140, 15),
                    Font = new Font("微软雅黑", 8F)
                };

                ProgressBar progress = new ProgressBar
                {
                    Location = new Point(5, 45),
                    Size = new Size(140, 15),
                    Maximum = challenge.Target
                };
                progress.Value = Math.Min(challenge.Current, challenge.Target);

                panel.Controls.Add(labelName);
                panel.Controls.Add(labelProgress);
                panel.Controls.Add(progress);

                if (challenge.Completed && !challenge.Claimed)
                {
                    Button claimBtn = new Button
                    {
                        Text = "领取",
                        Size = new Size(50, 20),
                        Location = new Point(95, 45)
                    };
                    var challengeCopy = challenge;
                    claimBtn.Click += (s, e) => ClaimReward(challengeCopy);
                    panel.Controls.Add(claimBtn);
                }

                _flowLayoutPanelChallenges.Controls.Add(panel);
            }
        }

        /// <summary>
        /// 获取所有挑战
        /// </summary>
        public IEnumerable<Challenge> GetAllChallenges() => _dailyChallenges;

        /// <summary>
        /// 获取已完成挑战数量
        /// </summary>
        public int CompletedCount => _dailyChallenges.Count(c => c.Completed);

        /// <summary>
        /// 获取已领取奖励的挑战数量
        /// </summary>
        public int ClaimedCount => _dailyChallenges.Count(c => c.Claimed);

        // JSON序列化辅助类
        private class ChallengeData
        {
            public string Date { get; set; } = "";
            public List<Challenge> Challenges { get; set; } = new();
        }
    }
}
