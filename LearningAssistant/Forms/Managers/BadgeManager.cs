using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    /// <summary>
    /// 徽章管理器 - 负责徽章定义、解锁检查和UI更新
    /// </summary>
    public class BadgeManager
    {
        private readonly ILogger<BadgeManager>? _logger;
        private readonly Dictionary<string, Badge> _badges = new();
        private readonly List<string> _unlockedBadges = new();
        private FlowLayoutPanel? _flowLayoutPanelBadges;
        private ToolTip? _toolTip;
        private bool _badgesEventBound = false;

        /// <summary>
        /// 徽章解锁事件
        /// </summary>
        public event Action<List<string>>? BadgesUnlocked;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BadgeManager(ILogger<BadgeManager>? logger = null)
        {
            _logger = logger;
            InitializeBadges();
        }

        /// <summary>
        /// 初始化所有徽章定义
        /// </summary>
        private void InitializeBadges()
        {
            _badges.Clear();
            _badges["first_blood"] = new Badge("first_blood", "首战告捷", "完成第一次学习", "🏆", 1);
            _badges["streak_3"] = new Badge("streak_3", "三日坚持", "连续学习3天", "🔥", 3);
            _badges["streak_7"] = new Badge("streak_7", "一周达人", "连续学习7天", "⭐", 7);
            _badges["streak_30"] = new Badge("streak_30", "月度冠军", "连续学习30天", "👑", 30);
            _badges["learn_100"] = new Badge("learn_100", "百题斩", "累计学习100项", "💯", 100);
            _badges["learn_500"] = new Badge("learn_500", "五百勇士", "累计学习500项", "⚔️", 500);
            _badges["learn_1000"] = new Badge("learn_1000", "千题大师", "累计学习1000项", "🏅", 1000);
            _badges["perfect_day"] = new Badge("perfect_day", "完美一天", "单日学习50项", "🌟", 50);
            _badges["quiz_master"] = new Badge("quiz_master", "答题高手", "答题模式答对20题", "🎯", 20);
            _badges["favorite_collector"] = new Badge("favorite_collector", "收藏达人", "收藏20个内容", "❤️", 20);
            _badges["note_taker"] = new Badge("note_taker", "笔记达人", "记录10条笔记", "📝", 10);
            _badges["speed_learner"] = new Badge("speed_learner", "神速学习", "5分钟内完成10项", "⚡", 10);
        }

        /// <summary>
        /// 设置UI控件引用
        /// </summary>
        public void SetUI(FlowLayoutPanel flowLayoutPanel, ToolTip toolTip)
        {
            _flowLayoutPanelBadges = flowLayoutPanel;
            _toolTip = toolTip;
        }

        /// <summary>
        /// 从文件加载已解锁的徽章
        /// </summary>
        public void Load()
        {
            try
            {
                string badgesPath = Path.Combine(AppPaths.DataDir, "badges.json");
                if (File.Exists(badgesPath))
                {
                    string json = File.ReadAllText(badgesPath);
                    _unlockedBadges.Clear();
                    var unlocked = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    _unlockedBadges.AddRange(unlocked);

                    foreach (var badgeId in _unlockedBadges)
                    {
                        if (_badges.TryGetValue(badgeId, out var badge))
                        {
                            badge.Unlocked = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载徽章失败");
            }
        }

        /// <summary>
        /// 保存已解锁的徽章到文件
        /// </summary>
        public void Save()
        {
            try
            {
                string badgesPath = Path.Combine(AppPaths.DataDir, "badges.json");
                var badgesDir = Path.GetDirectoryName(badgesPath);
                if (!string.IsNullOrEmpty(badgesDir) && !Directory.Exists(badgesDir))
                    Directory.CreateDirectory(badgesDir);

                string json = JsonSerializer.Serialize(_unlockedBadges, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(badgesPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存徽章失败");
            }
        }

        /// <summary>
        /// 检查并解锁达成的徽章
        /// </summary>
        public void CheckUnlock(int totalLearned, int streakDays, int todayLearned, int quizCorrect, int favoriteCount, int noteCount)
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

            if (newlyUnlocked.Count > 0)
            {
                Save();
                UpdateDisplay();
                BadgesUnlocked?.Invoke(newlyUnlocked);
            }
        }

        /// <summary>
        /// 尝试解锁徽章
        /// </summary>
        private void TryUnlockBadge(string badgeId, bool condition, List<string> newlyUnlocked)
        {
            if (condition && _badges.TryGetValue(badgeId, out var badge) && !badge.Unlocked)
            {
                UnlockBadge(badgeId, newlyUnlocked);
            }
        }

        /// <summary>
        /// 解锁指定徽章
        /// </summary>
        private void UnlockBadge(string badgeId, List<string> newlyUnlocked)
        {
            if (_badges.TryGetValue(badgeId, out var badge))
            {
                badge.Unlocked = true;
                _unlockedBadges.Add(badgeId);
                newlyUnlocked.Add(badgeId);
            }
        }

        /// <summary>
        /// 显示徽章解锁通知
        /// </summary>
        public void ShowNotification(List<string> badges)
        {
            string message = "🎉 解锁成就！\n\n";
            foreach (var badgeId in badges)
            {
                if (_badges.TryGetValue(badgeId, out var badge))
                {
                    message += $"{badge.Emoji} {badge.Name}\n{badge.Description}\n\n";
                }
            }
            message += "获得 50 积分奖励！";
            MessageBox.Show(message, "成就解锁", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 更新徽章显示面板
        /// </summary>
        public void UpdateDisplay()
        {
            if (_flowLayoutPanelBadges == null) return;

            _flowLayoutPanelBadges.Controls.Clear();
            foreach (var badge in _badges.Values)
            {
                Label label = new Label
                {
                    Font = new Font("微软雅黑", 14F),
                    Text = badge.Unlocked ? badge.Emoji : "🔒",
                    Size = new Size(40, 40),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    Tag = badge
                };
                label.Click += Badge_Click;
                _toolTip?.SetToolTip(label, badge.Unlocked ? $"{badge.Name}: {badge.Description}" : "未解锁");
                _flowLayoutPanelBadges.Controls.Add(label);
            }
        }

        /// <summary>
        /// 徽章点击事件处理器
        /// </summary>
        private void Badge_Click(object? sender, EventArgs e)
        {
            if (sender is Label label && label.Tag is Badge badge)
            {
                MessageBox.Show(
                    $"{badge.Emoji} {badge.Name}\n\n{badge.Description}",
                    badge.Unlocked ? "成就详情" : "锁定的成就",
                    MessageBoxButtons.OK,
                    badge.Unlocked ? MessageBoxIcon.Information : MessageBoxIcon.Question);
            }
        }

        /// <summary>
        /// 获取已解锁徽章数量
        /// </summary>
        public int UnlockedCount => _unlockedBadges.Count;

        /// <summary>
        /// 获取徽章总数
        /// </summary>
        public int TotalCount => _badges.Count;

        /// <summary>
        /// 获取所有徽章
        /// </summary>
        public IEnumerable<Badge> AllBadges => _badges.Values;
    }
}
