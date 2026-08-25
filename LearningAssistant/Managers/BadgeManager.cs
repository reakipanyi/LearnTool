using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    public class BadgeManager
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<BadgeManager>? _logger;
        private readonly IDialogService? _dialogService;
        private readonly IAppPaths? _appPaths;
        private string _currentUserId = Constants.DefaultUserId;
        private readonly Dictionary<string, Badge> _badges = new();
        private readonly List<string> _unlockedBadges = new();
        private FlowLayoutPanel? _flowLayoutPanelBadges;
        private ToolTip? _toolTip;
        private bool _badgesEventBound = false;

        public event Action<List<string>>? BadgesUnlocked;

        public BadgeManager(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<BadgeManager>? logger = null, IDialogService? dialogService = null, IAppPaths? appPaths = null)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
            _dialogService = dialogService;
            _appPaths = appPaths;
            InitializeBadges();
        }

        private void ShowMessage(string title, string message)
        {
            if (_dialogService != null)
                _dialogService.ShowMessageAsync(title, message).GetAwaiter().GetResult();
            else
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitializeBadges()
        {
            _badges.Clear();
            _badges["first_blood"] = new Badge
            {
                Id = "first_blood",
                Name = "首战告捷",
                Description = "完成第一次学习",
                Icon = "🏆",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 1 }
            };
            _badges["streak_3"] = new Badge
            {
                Id = "streak_3",
                Name = "三日坚持",
                Description = "连续学习3天",
                Icon = "🔥",
                Category = BadgeCategory.Consistency,
                Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 3 }
            };
            _badges["streak_7"] = new Badge
            {
                Id = "streak_7",
                Name = "一周达人",
                Description = "连续学习7天",
                Icon = "⭐",
                Category = BadgeCategory.Consistency,
                Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 7 }
            };
            _badges["streak_30"] = new Badge
            {
                Id = "streak_30",
                Name = "月度冠军",
                Description = "连续学习30天",
                Icon = "👑",
                Category = BadgeCategory.Consistency,
                Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 30 }
            };
            _badges["learn_100"] = new Badge
            {
                Id = "learn_100",
                Name = "百题斩",
                Description = "累计学习100项",
                Icon = "💯",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 100 }
            };
            _badges["learn_500"] = new Badge
            {
                Id = "learn_500",
                Name = "五百勇士",
                Description = "累计学习500项",
                Icon = "⚔️",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 500 }
            };
            _badges["learn_1000"] = new Badge
            {
                Id = "learn_1000",
                Name = "千题大师",
                Description = "累计学习1000项",
                Icon = "🏅",
                Category = BadgeCategory.Mastery,
                Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 1000 }
            };
            _badges["perfect_day"] = new Badge
            {
                Id = "perfect_day",
                Name = "完美一天",
                Description = "单日学习50项",
                Icon = "🌟",
                Category = BadgeCategory.Special,
                Requirement = new BadgeRequirement { Type = BadgeType.PerfectSession, TargetValue = 50 }
            };
            _badges["quiz_master"] = new Badge
            {
                Id = "quiz_master",
                Name = "答题高手",
                Description = "答题模式答对20题",
                Icon = "🎯",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.QuizCorrect, TargetValue = 20 }
            };
            _badges["favorite_collector"] = new Badge
            {
                Id = "favorite_collector",
                Name = "收藏达人",
                Description = "收藏20个内容",
                Icon = "❤️",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.FavoriteCount, TargetValue = 20 }
            };
            _badges["note_taker"] = new Badge
            {
                Id = "note_taker",
                Name = "笔记达人",
                Description = "记录10条笔记",
                Icon = "📝",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.NoteCount, TargetValue = 10 }
            };
            _badges["speed_learner"] = new Badge
            {
                Id = "speed_learner",
                Name = "神速学习",
                Description = "5分钟内完成10项",
                Icon = "⚡",
                Category = BadgeCategory.Special,
                Requirement = new BadgeRequirement { Type = BadgeType.SpeedLearning, TargetValue = 10 }
            };
            _badges["time_investor"] = new Badge
            {
                Id = "time_investor",
                Name = "时间投资者",
                Description = "累计学习超过10小时",
                Icon = "⏰",
                Category = BadgeCategory.Learning,
                Requirement = new BadgeRequirement { Type = BadgeType.TotalStudyTime, TargetValue = 600 }
            };
            _badges["time_master"] = new Badge
            {
                Id = "time_master",
                Name = "时间大师",
                Description = "累计学习超过100小时",
                Icon = "🎓",
                Category = BadgeCategory.Mastery,
                Requirement = new BadgeRequirement { Type = BadgeType.TotalStudyTime, TargetValue = 6000 }
            };
            _badges["night_owl"] = new Badge
            {
                Id = "night_owl",
                Name = "夜猫子",
                Description = "在深夜时段（00:00-05:00）完成学习",
                Icon = "🌙",
                Category = BadgeCategory.Special,
                IsHidden = true,
                Requirement = new BadgeRequirement { Type = BadgeType.NightStudy, TargetValue = 1 }
            };
            _badges["easter_egg_master"] = new Badge
            {
                Id = "easter_egg_master",
                Name = "彩蛋猎人",
                Description = "发现并解锁一个隐藏成就",
                Icon = "🥚",
                Category = BadgeCategory.Special,
                IsHidden = true,
                Requirement = new BadgeRequirement { Type = BadgeType.HiddenBadgeCount, TargetValue = 1 }
            };
        }

        public void SetUI(FlowLayoutPanel flowLayoutPanel, ToolTip toolTip)
        {
            _flowLayoutPanelBadges = flowLayoutPanel;
            _toolTip = toolTip;
        }

        public void Load(string userId = "default")
        {
            _currentUserId = userId;
            try
            {
                MigrateFromJsonToDb(userId);

                using var db = _dbContextFactory.CreateDbContext();
                var unlocked = db.BadgeUnlocks
                    .Where(b => b.UserId == userId)
                    .Select(b => b.BadgeId)
                    .ToList();

                _unlockedBadges.Clear();
                _unlockedBadges.AddRange(unlocked);

                foreach (var badge in _badges.Values)
                {
                    badge.IsUnlocked = _unlockedBadges.Contains(badge.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载徽章失败");
            }
        }

        private void MigrateFromJsonToDb(string userId)
        {
            try
            {
                var userDir = Path.Combine(_appPaths?.UsersDir ?? AppPaths.UsersDir, userId);
                if (!Directory.Exists(userDir)) return;

                var migratedMarker = Path.Combine(userDir, ".badges_migrated");
                if (File.Exists(migratedMarker)) return;

                var badgesPath = Path.Combine(userDir, "badges.json");
                if (!File.Exists(badgesPath)) return;

                var json = File.ReadAllText(badgesPath);
                var unlocked = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                using var db = _dbContextFactory.CreateDbContext();

                var existingIds = db.BadgeUnlocks
                    .Where(b => b.UserId == userId)
                    .Select(b => b.BadgeId)
                    .ToHashSet();

                foreach (var badgeId in unlocked)
                {
                    if (existingIds.Contains(badgeId)) continue;

                    db.BadgeUnlocks.Add(new BadgeUnlockEntity
                    {
                        UserId = userId,
                        BadgeId = badgeId,
                        UnlockedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
                File.Create(migratedMarker).Dispose();
                _logger?.LogInformation("迁移徽章数据从JSON到数据库完成: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "迁移徽章数据失败: {UserId}", userId);
            }
        }

        public void Save(string userId = Constants.DefaultUserId)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                var existingIds = db.BadgeUnlocks
                    .Where(b => b.UserId == userId)
                    .Select(b => b.BadgeId)
                    .ToHashSet();

                foreach (var badgeId in _unlockedBadges)
                {
                    if (existingIds.Contains(badgeId)) continue;

                    db.BadgeUnlocks.Add(new BadgeUnlockEntity
                    {
                        UserId = userId,
                        BadgeId = badgeId,
                        UnlockedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存徽章失败");
            }
        }

        public void CheckUnlock(int totalLearned, int streakDays, int todayLearned, int quizCorrect, int favoriteCount, int noteCount,
            int totalStudyMinutes = 0, int speedLearningCount = 0, int nightStudyCount = 0)
        {
            List<string> newlyUnlocked = new();

            TryUnlockBadge("first_blood", totalLearned >= 1, newlyUnlocked);
            TryUnlockBadge("streak_3", streakDays >= 3, newlyUnlocked);
            TryUnlockBadge("streak_7", streakDays >= 7, newlyUnlocked);
            TryUnlockBadge("streak_30", streakDays >= 30, newlyUnlocked);
            TryUnlockBadge("learn_100", totalLearned >= 100, newlyUnlocked);
            TryUnlockBadge("learn_500", totalLearned >= 500, newlyUnlocked);
            TryUnlockBadge("learn_1000", totalLearned >= 1000, newlyUnlocked);
            TryUnlockBadge("perfect_day", todayLearned >= 50, newlyUnlocked);
            TryUnlockBadge("quiz_master", quizCorrect >= 20, newlyUnlocked);
            TryUnlockBadge("favorite_collector", favoriteCount >= 20, newlyUnlocked);
            TryUnlockBadge("note_taker", noteCount >= 10, newlyUnlocked);
            TryUnlockBadge("speed_learner", speedLearningCount >= 10, newlyUnlocked);
            TryUnlockBadge("time_investor", totalStudyMinutes >= 600, newlyUnlocked);
            TryUnlockBadge("time_master", totalStudyMinutes >= 6000, newlyUnlocked);
            TryUnlockBadge("night_owl", nightStudyCount >= 1, newlyUnlocked);

            // 彩蛋猎人：解锁任意隐藏成就后自动解锁
            int hiddenUnlockedCount = _badges.Values
                .Count(b => b.IsHidden && b.IsUnlocked && b.Id != "easter_egg_master");
            TryUnlockBadge("easter_egg_master", hiddenUnlockedCount >= 1, newlyUnlocked);

            if (newlyUnlocked.Count > 0)
            {
                Save(_currentUserId);
                UpdateDisplay();
                BadgesUnlocked?.Invoke(newlyUnlocked);
            }
        }

        private void TryUnlockBadge(string badgeId, bool condition, List<string> newlyUnlocked)
        {
            if (condition && _badges.TryGetValue(badgeId, out var badge) && !badge.IsUnlocked)
            {
                UnlockBadge(badgeId, newlyUnlocked);
            }
        }

        private void UnlockBadge(string badgeId, List<string> newlyUnlocked)
        {
            if (_badges.TryGetValue(badgeId, out var badge))
            {
                badge.IsUnlocked = true;
                badge.UnlockedAt = DateTime.Now;
                _unlockedBadges.Add(badgeId);
                newlyUnlocked.Add(badgeId);
            }
        }

        public void ShowNotification(List<string> badges)
        {
            string message = "🎉 解锁成就！\n\n";
            foreach (var badgeId in badges)
            {
                if (_badges.TryGetValue(badgeId, out var badge))
                {
                    message += $"{badge.Icon} {badge.Name}\n{badge.Description}\n\n";
                }
            }
            message += "获得 50 积分奖励！";
            ShowMessage("成就解锁", message);
        }

        public void UpdateDisplay()
        {
            if (_flowLayoutPanelBadges == null) return;

            _flowLayoutPanelBadges.Controls.Clear();
            foreach (var badge in _badges.Values)
            {
                Label label = new Label
                {
                    Font = new Font("微软雅黑", 14F),
                    Text = badge.IsUnlocked ? badge.Icon : "🔒",
                    Size = new Size(40, 40),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    Tag = badge
                };
                label.Click += Badge_Click;
                _toolTip?.SetToolTip(label, badge.IsUnlocked ? $"{badge.Name}: {badge.Description}" : "未解锁");
                _flowLayoutPanelBadges.Controls.Add(label);
            }
        }

        private void Badge_Click(object? sender, EventArgs e)
        {
            if (sender is Label label && label.Tag is Badge badge)
            {
                ShowMessage(
                    badge.IsUnlocked ? "成就详情" : "锁定的成就",
                    $"{badge.Icon} {badge.Name}\n\n{badge.Description}");
            }
        }

        public int UnlockedCount => _unlockedBadges.Count;

        public int TotalCount => _badges.Count;

        public IEnumerable<Badge> AllBadges => _badges.Values;

        public Dictionary<string, int> GetProgressDictionary(
            int totalLearned,
            int streakDays,
            int totalStudyMinutes,
            int quizCorrect,
            int favoriteCount,
            int noteCount,
            int perfectSessions = 0,
            int speedLearningCount = 0,
            int nightStudyCount = 0)
        {
            var progress = new Dictionary<string, int>();
            foreach (var badge in _badges.Values)
            {
                progress[badge.Id] = GetBadgeProgress(badge, totalLearned, streakDays,
                    totalStudyMinutes, quizCorrect, favoriteCount, noteCount,
                    perfectSessions, speedLearningCount, nightStudyCount);
            }
            return progress;
        }

        private int GetBadgeProgress(
            Badge badge,
            int totalLearned,
            int streakDays,
            int totalStudyMinutes,
            int quizCorrect,
            int favoriteCount,
            int noteCount,
            int perfectSessions,
            int speedLearningCount,
            int nightStudyCount = 0)
        {
            return badge.Requirement.Type switch
            {
                BadgeType.ConsecutiveDays => streakDays,
                BadgeType.TotalStudyTime => totalStudyMinutes,
                BadgeType.TotalItemsLearned => totalLearned,
                BadgeType.PerfectSession => perfectSessions,
                BadgeType.QuizCorrect => quizCorrect,
                BadgeType.FavoriteCount => favoriteCount,
                BadgeType.NoteCount => noteCount,
                BadgeType.SpeedLearning => speedLearningCount,
                BadgeType.NightStudy => nightStudyCount,
                BadgeType.HiddenBadgeCount => _badges.Values.Count(b => b.IsHidden && b.IsUnlocked && b.Id != "easter_egg_master"),
                _ => 0
            };
        }
    }
}
