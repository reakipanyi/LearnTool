using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// 挑战管理器 - 负责每日挑战任务的生成、进度追踪、奖励领取和历史记录
    /// </summary>
    public class ChallengeManager
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<ChallengeManager>? _logger;
        private readonly Action<int>? _onScoreChanged;
        private readonly Action<int>? _onXPChanged;
        private readonly Action? _onLevelUp;
        private readonly Action? _onChallengeCompleted;
        private readonly IAppPaths? _appPaths;

        private string _currentUserId = Constants.DefaultUserId;
        private List<Challenge> _dailyChallenges = new();
        private List<ChallengeHistoryRecord> _historyRecords = new();
        private FlowLayoutPanel? _flowLayoutPanelChallenges;
        private ISoundService? _soundService;

        // 学习数据（由外部传入或从服务获取）
        private int _todayLearnedCount = 0;
        private int _quizCorrectCount = 0;
        private int _quizTotalCount = 0;
        private int _favoriteCount = 0;
        private int _todayStudyMinutes = 0;
        private int _streakDays = 0;
        private int _wrongReviewCount = 0;
        private int _noteCount = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChallengeManager(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<ChallengeManager>? logger = null,
            Action<int>? onScoreChanged = null,
            Action<int>? onXPChanged = null,
            Action? onLevelUp = null,
            Action? onChallengeCompleted = null,
            IAppPaths? appPaths = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
            _onScoreChanged = onScoreChanged;
            _onXPChanged = onXPChanged;
            _onLevelUp = onLevelUp;
            _onChallengeCompleted = onChallengeCompleted;
            _appPaths = appPaths;
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
        /// 设置当前学习数据（扩展版）
        /// </summary>
        public void SetLearningData(
            int todayLearnedCount,
            int quizCorrectCount,
            int favoriteCount,
            int todayStudyMinutes = 0,
            int streakDays = 0,
            int wrongReviewCount = 0,
            int noteCount = 0,
            int quizTotalCount = 0)
        {
            _todayLearnedCount = todayLearnedCount;
            _quizCorrectCount = quizCorrectCount;
            _quizTotalCount = quizTotalCount;
            _favoriteCount = favoriteCount;
            _todayStudyMinutes = todayStudyMinutes;
            _streakDays = streakDays;
            _wrongReviewCount = wrongReviewCount;
            _noteCount = noteCount;
        }

        /// <summary>
        /// 加载每日挑战和历史记录
        /// </summary>
        public void Load(string userId = "default")
        {
            _currentUserId = userId;
            try
            {
                MigrateFromJsonToDb(userId);
                LoadHistory(userId);

                string today = DateTime.Today.ToString("yyyy-MM-dd");

                using var db = _dbContextFactory.CreateDbContext();
                var entity = db.DailyChallenges.FirstOrDefault(d => d.UserId == userId && d.Date == today);

                if (entity != null)
                {
                    _dailyChallenges = !string.IsNullOrEmpty(entity.ChallengesJson)
                        ? System.Text.Json.JsonSerializer.Deserialize<List<Challenge>>(entity.ChallengesJson) ?? new List<Challenge>()
                        : new List<Challenge>();
                }
                else
                {
                    var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                    var yesterdayEntity = db.DailyChallenges.FirstOrDefault(d => d.UserId == userId && d.Date == yesterday);
                    if (yesterdayEntity != null && !string.IsNullOrEmpty(yesterdayEntity.ChallengesJson))
                    {
                        var yesterdayChallenges = System.Text.Json.JsonSerializer.Deserialize<List<Challenge>>(yesterdayEntity.ChallengesJson) ?? new List<Challenge>();
                        SaveToHistory(yesterday, yesterdayChallenges);
                    }
                    GenerateDailyChallenges();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载挑战失败");
                GenerateDailyChallenges();
            }
        }

        private void MigrateFromJsonToDb(string userId)
        {
            try
            {
                var userDir = Path.Combine(_appPaths?.UsersDir ?? AppPaths.UsersDir, userId);
                if (!Directory.Exists(userDir)) return;

                var migratedMarker = Path.Combine(userDir, ".challenges_migrated");
                if (File.Exists(migratedMarker)) return;

                using var db = _dbContextFactory.CreateDbContext();

                var challengesPath = Path.Combine(userDir, "challenges.json");
                if (File.Exists(challengesPath))
                {
                    var json = File.ReadAllText(challengesPath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<ChallengeData>(json);

                    if (data != null)
                    {
                        var existing = db.DailyChallenges.FirstOrDefault(d => d.UserId == userId && d.Date == data.Date);
                        if (existing == null)
                        {
                            db.DailyChallenges.Add(new DailyChallengeEntity
                            {
                                UserId = userId,
                                Date = data.Date,
                                ChallengesJson = System.Text.Json.JsonSerializer.Serialize(data.Challenges)
                            });
                            db.SaveChanges();
                        }
                    }
                }

                var historyPath = Path.Combine(userDir, "challenge_history.json");
                if (File.Exists(historyPath))
                {
                    var json = File.ReadAllText(historyPath);
                    var records = System.Text.Json.JsonSerializer.Deserialize<List<ChallengeHistoryRecord>>(json) ?? new List<ChallengeHistoryRecord>();

                    var existingDates = db.ChallengeHistory.Where(h => h.UserId == userId).Select(h => h.Date).ToHashSet();

                    foreach (var record in records)
                    {
                        if (existingDates.Contains(record.Date)) continue;

                        db.ChallengeHistory.Add(new ChallengeHistoryEntity
                        {
                            UserId = userId,
                            Date = record.Date,
                            CompletedCount = record.CompletedCount,
                            TotalCount = record.TotalCount,
                            ClaimedCount = record.ClaimedCount,
                            TotalXP = record.TotalXP,
                            ChallengesJson = System.Text.Json.JsonSerializer.Serialize(record.Challenges)
                        });
                    }

                    db.SaveChanges();
                }

                File.Create(migratedMarker).Dispose();
                _logger?.LogInformation("迁移挑战数据从JSON到数据库完成: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "迁移挑战数据失败: {UserId}", userId);
            }
        }

        /// <summary>
        /// 加载挑战历史记录
        /// </summary>
        private void LoadHistory(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                _historyRecords = db.ChallengeHistory
                    .Where(h => h.UserId == userId)
                    .Select(e => new ChallengeHistoryRecord
                    {
                        Date = e.Date,
                        CompletedCount = e.CompletedCount,
                        TotalCount = e.TotalCount,
                        ClaimedCount = e.ClaimedCount,
                        TotalXP = e.TotalXP,
                        Challenges = !string.IsNullOrEmpty(e.ChallengesJson)
                            ? System.Text.Json.JsonSerializer.Deserialize<List<ChallengeSummary>>(e.ChallengesJson) ?? new List<ChallengeSummary>()
                            : new List<ChallengeSummary>()
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载挑战历史失败");
                _historyRecords = new();
            }
        }

        /// <summary>
        /// 保存当天的完成情况到历史记录
        /// </summary>
        private void SaveToHistory(string date, List<Challenge> challenges)
        {
            int completed = challenges.Count(c => c.Completed);
            int claimed = challenges.Count(c => c.Claimed);
            int totalXP = challenges.Where(c => c.Claimed).Sum(c => c.Reward);

            var record = new ChallengeHistoryRecord
            {
                Date = date,
                CompletedCount = completed,
                TotalCount = challenges.Count,
                ClaimedCount = claimed,
                TotalXP = totalXP,
                Challenges = challenges.Select(c => new ChallengeSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    Completed = c.Completed,
                    Claimed = c.Claimed
                }).ToList()
            };

            // 检查是否已有记录，更新或添加
            var existing = _historyRecords.FirstOrDefault(h => h.Date == date);
            if (existing != null)
            {
                _historyRecords.Remove(existing);
            }
            _historyRecords.Add(record);

            // 只保留最近60天的记录
            _historyRecords = _historyRecords
                .OrderByDescending(h => h.Date)
                .Take(60)
                .ToList();

            SaveHistory(_currentUserId);
        }

        /// <summary>
        /// 保存历史记录
        /// </summary>
        private void SaveHistory(string userId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                foreach (var record in _historyRecords)
                {
                    var existing = db.ChallengeHistory.FirstOrDefault(e => e.UserId == userId && e.Date == record.Date);
                    if (existing != null)
                    {
                        existing.CompletedCount = record.CompletedCount;
                        existing.TotalCount = record.TotalCount;
                        existing.ClaimedCount = record.ClaimedCount;
                        existing.TotalXP = record.TotalXP;
                        existing.ChallengesJson = System.Text.Json.JsonSerializer.Serialize(record.Challenges);
                    }
                    else
                    {
                        db.ChallengeHistory.Add(new ChallengeHistoryEntity
                        {
                            UserId = userId,
                            Date = record.Date,
                            CompletedCount = record.CompletedCount,
                            TotalCount = record.TotalCount,
                            ClaimedCount = record.ClaimedCount,
                            TotalXP = record.TotalXP,
                            ChallengesJson = System.Text.Json.JsonSerializer.Serialize(record.Challenges)
                        });
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存挑战历史失败");
            }
        }

        /// <summary>
        /// 生成每日挑战（扩展版，根据用户近期完成率自适应难度）
        /// </summary>
        private void GenerateDailyChallenges()
        {
            var allTemplates = new List<ChallengeTemplate>
            {
                new ChallengeTemplate("daily_learn", "📚 每日学习", "完成 {0} 个学习项", "📚", new[] { 10, 15, 20 }, new[] { 20, 30, 40 }),
                new ChallengeTemplate("daily_time", "⏱️ 学习时长", "今日学习满 {0} 分钟", "⏱️", new[] { 15, 30, 45 }, new[] { 15, 30, 50 }),
                new ChallengeTemplate("daily_accuracy", "🎯 正确率挑战", "今日正确率 ≥ {0}%", "🎯", new[] { 70, 80, 90 }, new[] { 20, 30, 40 }),
                new ChallengeTemplate("daily_streak", "🔥 连续打卡", "保持连续学习", "🔥", new[] { 1 }, new[] { 30 }),
                new ChallengeTemplate("daily_wrong", "📕 错题复习", "复习 {0} 道错题", "📕", new[] { 3, 5, 10 }, new[] { 15, 25, 35 }),
                new ChallengeTemplate("daily_note", "📝 记笔记", "今日新增 {0} 条笔记", "📝", new[] { 1, 3, 5 }, new[] { 10, 15, 20 }),
                new ChallengeTemplate("daily_favorite", "❤️ 收藏内容", "收藏 {0} 个内容", "❤️", new[] { 3, 5 }, new[] { 15, 25 })
            };

            // 随机选择4-5个挑战
            var selectedTemplates = allTemplates
                .OrderBy(t => Guid.NewGuid())
                .Take(5)
                .ToList();

            // 根据用户近7天挑战完成率自适应选择难度
            double recentRate = GetRecentCompletionRate();
            int baseDifficultyIndex = recentRate switch
            {
                >= 0.8 => 2,  // 高完成率 → 高难度
                >= 0.5 => 1,  // 中等完成率 → 中难度
                _ => 0         // 低完成率 → 低难度
            };

            _dailyChallenges = new List<Challenge>();

            for (int i = 0; i < selectedTemplates.Count; i++)
            {
                var template = selectedTemplates[i];
                // 在基础难度上交替分布，保证挑战难度多样性
                int difficultyIndex = (baseDifficultyIndex + i) % 3;
                difficultyIndex = Math.Clamp(difficultyIndex, 0, template.Targets.Length - 1);
                int target = template.Targets[difficultyIndex];
                int reward = template.Rewards[Math.Min(difficultyIndex, template.Rewards.Length - 1)];

                string description = template.DescriptionTemplate.Replace("{0}", target.ToString());

                var challenge = new Challenge(
                    template.Id,
                    template.Name,
                    description,
                    template.Emoji,
                    target,
                    reward)
                {
                    Type = GetChallengeType(template.Id)
                };

                _dailyChallenges.Add(challenge);
            }

            // 添加"完美一天"隐藏挑战（完成所有挑战后自动完成）
            _dailyChallenges.Add(new Challenge("daily_perfect", "🏆 完美一天", "完成所有每日挑战", "🏆", _dailyChallenges.Count, 50)
            {
                Type = ChallengeType.Custom,
                Completed = false
            });

            Save(_currentUserId);
        }

        /// <summary>
        /// 计算近7天挑战平均完成率，用于难度自适应
        /// </summary>
        private double GetRecentCompletionRate()
        {
            if (_historyRecords.Count == 0) return 0.3; // 新用户默认低难度

            var recent = _historyRecords
                .OrderByDescending(h => h.Date)
                .Take(7)
                .ToList();

            if (recent.Count == 0) return 0.3;

            return recent.Average(h => h.TotalCount > 0 ? (double)h.CompletedCount / h.TotalCount : 0);
        }

        private ChallengeType GetChallengeType(string id)
        {
            return id switch
            {
                "daily_learn" => ChallengeType.LearnItems,
                "daily_time" => ChallengeType.StudyTime,
                "daily_wrong" => ChallengeType.WrongItems,
                "daily_accuracy" => ChallengeType.Custom,
                "daily_streak" => ChallengeType.Custom,
                "daily_note" => ChallengeType.Custom,
                "daily_favorite" => ChallengeType.Custom,
                _ => ChallengeType.Custom
            };
        }

        /// <summary>
        /// 保存挑战数据
        /// </summary>
        public void Save(string userId = Constants.DefaultUserId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                var existing = db.DailyChallenges.FirstOrDefault(d => d.UserId == userId && d.Date == today);
                if (existing != null)
                {
                    existing.ChallengesJson = System.Text.Json.JsonSerializer.Serialize(_dailyChallenges);
                }
                else
                {
                    db.DailyChallenges.Add(new DailyChallengeEntity
                    {
                        UserId = userId,
                        Date = today,
                        ChallengesJson = System.Text.Json.JsonSerializer.Serialize(_dailyChallenges)
                    });
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存挑战失败");
            }
        }

        /// <summary>
        /// 更新所有挑战的进度（扩展版）
        /// </summary>
        public void UpdateProgress()
        {
            bool anyNewlyCompleted = false;

            foreach (var challenge in _dailyChallenges)
            {
                if (challenge.Id == "daily_perfect") continue; // 特殊处理

                int previousCurrent = challenge.Current;

                switch (challenge.Id)
                {
                    case "daily_learn":
                        challenge.Current = _todayLearnedCount;
                        break;
                    case "daily_time":
                        challenge.Current = _todayStudyMinutes;
                        break;
                    case "daily_accuracy":
                        double accuracy = _quizTotalCount > 0 ? (double)_quizCorrectCount / _quizTotalCount * 100 : 0;
                        challenge.Current = (int)accuracy;
                        break;
                    case "daily_streak":
                        challenge.Current = _streakDays > 0 ? 1 : 0;
                        break;
                    case "daily_wrong":
                        challenge.Current = _wrongReviewCount;
                        break;
                    case "daily_note":
                        challenge.Current = _noteCount;
                        break;
                    case "daily_favorite":
                        challenge.Current = _favoriteCount;
                        break;
                    case "daily_quiz":
                        challenge.Current = _quizCorrectCount;
                        break;
                }

                // 检查是否刚完成
                if (!challenge.Completed && challenge.Current >= challenge.Target)
                {
                    challenge.Completed = true;
                    anyNewlyCompleted = true;
                    _onChallengeCompleted?.Invoke();
                }
            }

            // 检查"完美一天"挑战
            var perfectChallenge = _dailyChallenges.FirstOrDefault(c => c.Id == "daily_perfect");
            if (perfectChallenge != null)
            {
                int completedNormal = _dailyChallenges.Count(c => c.Completed && c.Id != "daily_perfect");
                int totalNormal = _dailyChallenges.Count(c => c.Id != "daily_perfect");

                perfectChallenge.Current = completedNormal;

                if (!perfectChallenge.Completed && completedNormal >= totalNormal)
                {
                    perfectChallenge.Completed = true;
                    anyNewlyCompleted = true;
                    _onChallengeCompleted?.Invoke();
                }
            }

            Save(_currentUserId);
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

            Save(_currentUserId);
            UpdateDisplay();
        }

        private void OnClaimRewardClick(object? sender, EventArgs e)
        {
            if (sender is Button claimBtn && claimBtn.Tag is Challenge challenge)
            {
                ClaimReward(challenge);
            }
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
                    BackColor = challenge.Completed ? Color.FromArgb(230, 255, 230) : SystemColors.Control
                };

                Label labelName = new Label
                {
                    Text = $"{challenge.Emoji} {challenge.Name}",
                    Location = new Point(5, 5),
                    Size = new Size(140, 15),
                    Font = new Font("微软雅黑", 9F)
                };

                Label labelProgress = new Label
                {
                    Text = $"{challenge.Current}/{challenge.Target}",
                    Location = new Point(5, 25),
                    Size = new Size(80, 15),
                    Font = new Font("微软雅黑", 8F)
                };

                Label labelReward = new Label
                {
                    Text = $"+{challenge.Reward} XP",
                    Location = new Point(90, 25),
                    Size = new Size(55, 15),
                    Font = new Font("微软雅黑", 8F),
                    ForeColor = Color.FromArgb(255, 152, 0),
                    TextAlign = ContentAlignment.TopRight
                };

                ProgressBar progress = new ProgressBar
                {
                    Location = new Point(5, 45),
                    Size = new Size(90, 15),
                    Maximum = challenge.Target
                };
                progress.Value = Math.Min(challenge.Current, challenge.Target);

                if (challenge.Completed && !challenge.Claimed)
                {
                    Button claimBtn = new Button
                    {
                        Text = "领取",
                        Size = new Size(45, 20),
                        Location = new Point(100, 43),
                        BackColor = Color.FromArgb(255, 152, 0),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Tag = challenge
                    };
                    claimBtn.FlatAppearance.BorderSize = 0;
                    claimBtn.Click += OnClaimRewardClick;
                    panel.Controls.Add(claimBtn);
                }
                else if (challenge.Claimed)
                {
                    Label labelClaimed = new Label
                    {
                        Text = "✓ 已领取",
                        Location = new Point(100, 45),
                        Size = new Size(45, 15),
                        Font = new Font("微软雅黑", 8F),
                        ForeColor = Color.FromArgb(76, 175, 80),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    panel.Controls.Add(labelClaimed);
                }

                panel.Controls.Add(labelName);
                panel.Controls.Add(labelProgress);
                panel.Controls.Add(labelReward);
                panel.Controls.Add(progress);

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

        /// <summary>
        /// 获取历史记录
        /// </summary>
        public IEnumerable<ChallengeHistoryRecord> GetHistory(int days = 30)
        {
            return _historyRecords.OrderByDescending(h => h.Date).Take(days);
        }

        /// <summary>
        /// 获取指定日期的历史记录
        /// </summary>
        public ChallengeHistoryRecord? GetHistoryByDate(string date)
        {
            return _historyRecords.FirstOrDefault(h => h.Date == date);
        }

        /// <summary>
        /// 计算历史统计
        /// </summary>
        public ChallengeHistoryStats GetHistoryStats()
        {
            if (_historyRecords.Count == 0)
            {
                return new ChallengeHistoryStats();
            }

            return new ChallengeHistoryStats
            {
                TotalDays = _historyRecords.Count,
                PerfectDays = _historyRecords.Count(h => h.CompletedCount == h.TotalCount),
                TotalXPClaimed = _historyRecords.Sum(h => h.TotalXP),
                AverageCompletionRate = _historyRecords.Average(h => (double)h.CompletedCount / h.TotalCount),
                RecentStreak = CalculateRecentStreak()
            };
        }

        private int CalculateRecentStreak()
        {
            int streak = 0;
            var sorted = _historyRecords.OrderByDescending(h => h.Date).ToList();

            foreach (var record in sorted)
            {
                var date = DateTime.Parse(record.Date);
                var expectedDate = DateTime.Today.AddDays(-streak);

                if (date == expectedDate && record.CompletedCount > 0)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        // JSON序列化辅助类
        private class ChallengeData
        {
            public string Date { get; set; } = "";
            public List<Challenge> Challenges { get; set; } = new();
        }
    }

    /// <summary>
    /// 挑战模板
    /// </summary>
    public class ChallengeTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DescriptionTemplate { get; set; }
        public string Emoji { get; set; }
        public int[] Targets { get; set; }
        public int[] Rewards { get; set; }

        public ChallengeTemplate(string id, string name, string description, string emoji, int[] targets, int[] rewards)
        {
            Id = id;
            Name = name;
            DescriptionTemplate = description;
            Emoji = emoji;
            Targets = targets;
            Rewards = rewards;
        }
    }

    /// <summary>
    /// 挑战历史记录
    /// </summary>
    public class ChallengeHistoryRecord
    {
        public string Date { get; set; } = "";
        public int CompletedCount { get; set; }
        public int TotalCount { get; set; }
        public int ClaimedCount { get; set; }
        public int TotalXP { get; set; }
        public List<ChallengeSummary> Challenges { get; set; } = new();
    }

    /// <summary>
    /// 挑战摘要（用于历史记录）
    /// </summary>
    public class ChallengeSummary
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Completed { get; set; }
        public bool Claimed { get; set; }
    }

    /// <summary>
    /// 挑战历史统计
    /// </summary>
    public class ChallengeHistoryStats
    {
        public int TotalDays { get; set; }
        public int PerfectDays { get; set; }
        public int TotalXPClaimed { get; set; }
        public double AverageCompletionRate { get; set; }
        public int RecentStreak { get; set; }
    }
}