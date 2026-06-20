using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// 统计数据管理器 - 负责学习统计数据的加载、保存和更新
    /// </summary>
    public class StudyStatsManager
    {
        private readonly ILogger<StudyStatsManager>? _logger;
        private readonly List<string> _levelTitles = new() {
            "小白", "学徒", "学者", "秀才", "举人", "进士", "翰林", "大师", "宗师", "圣人"
        };

        // 当前统计数据
        private int _todayLearnedCount = 0;
        private int _streakDays = 0;
        private int _totalScore = 0;
        private int _totalLearnedCount = 0;
        private int _xp = 0;
        private int _currentLevel = 0;
        private int _xpToNextLevel = 100;
        private string _levelTitle = "小白";
        private DateTime _lastStudyDate = DateTime.MinValue;

        // UI 控件引用
        private Label? _labelStudyTime;
        private Label? _labelScore;
        private Label? _labelTodayCount;
        private Label? _labelStreak;
        private Label? _labelLevel;
        private Label? _labelXP;
        private ProgressBar? _progressXP;

        // 回调函数
        private readonly Action? _onLevelUp;
        private readonly Action<int>? _onScoreChanged;
        private readonly Action<int>? _onXPChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public StudyStatsManager(
            ILogger<StudyStatsManager>? logger = null,
            Action? onLevelUp = null,
            Action<int>? onScoreChanged = null,
            Action<int>? onXPChanged = null)
        {
            _logger = logger;
            _onLevelUp = onLevelUp;
            _onScoreChanged = onScoreChanged;
            _onXPChanged = onXPChanged;
        }

        /// <summary>
        /// 设置UI控件引用
        /// </summary>
        public void SetUI(
            Label labelStudyTime,
            Label labelScore,
            Label labelTodayCount,
            Label labelStreak,
            Label labelLevel,
            Label labelXP,
            ProgressBar progressXP)
        {
            _labelStudyTime = labelStudyTime;
            _labelScore = labelScore;
            _labelTodayCount = labelTodayCount;
            _labelStreak = labelStreak;
            _labelLevel = labelLevel;
            _labelXP = labelXP;
            _progressXP = progressXP;
        }

        /// <summary>
        /// 从文件加载统计数据
        /// </summary>
        public void Load()
        {
            try
            {
                string statsPath = Path.Combine(AppPaths.DataDir, "study_stats.json");
                if (File.Exists(statsPath))
                {
                    string json = File.ReadAllText(statsPath);
                    var stats = JsonSerializer.Deserialize<StudyStats>(json);

                    if (stats != null)
                    {
                        _todayLearnedCount = stats.TodayLearnedCount;
                        _streakDays = stats.StreakDays;
                        _totalScore = stats.TotalScore;
                        _totalLearnedCount = stats.TotalLearnedCount;
                        _xp = stats.XP;
                        _lastStudyDate = stats.LastStudyDate;

                        // 检查是否需要重置今日学习数
                        if (_lastStudyDate.Date != DateTime.Today)
                        {
                            _todayLearnedCount = 0;
                            // 检查是否连续学习
                            if (_lastStudyDate.Date == DateTime.Today.AddDays(-1))
                            {
                                _streakDays++;
                            }
                            else if (_lastStudyDate.Date != DateTime.MinValue)
                            {
                                _streakDays = 1;
                            }
                        }

                        UpdateLevel();
                    }
                }
                else
                {
                    _streakDays = 1;
                }

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载学习统计失败");
                _streakDays = 1;
            }
        }

        /// <summary>
        /// 保存统计数据到文件
        /// </summary>
        public void Save()
        {
            try
            {
                string statsPath = Path.Combine(AppPaths.DataDir, "study_stats.json");
                var statsDir = Path.GetDirectoryName(statsPath);
                if (!string.IsNullOrEmpty(statsDir) && !Directory.Exists(statsDir))
                    Directory.CreateDirectory(statsDir);

                var stats = new StudyStats
                {
                    TodayLearnedCount = _todayLearnedCount,
                    StreakDays = _streakDays,
                    TotalScore = _totalScore,
                    TotalLearnedCount = _totalLearnedCount,
                    XP = _xp,
                    LastStudyDate = DateTime.Today
                };

                string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(statsPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存学习统计失败");
            }
        }

        /// <summary>
        /// 更新学习时长显示
        /// </summary>
        public void UpdateStudyTime(TimeSpan duration)
        {
            if (_labelStudyTime != null)
            {
                _labelStudyTime.Text = $"⏱️ 学习时长: {duration:hh\\:mm\\:ss}";
            }
        }

        /// <summary>
        /// 增加分数
        /// </summary>
        public void AddScore(int points)
        {
            _totalScore += points;
            UpdateDisplay();
        }

        /// <summary>
        /// 增加学习项计数
        /// </summary>
        public void IncrementLearnedCount()
        {
            _todayLearnedCount++;
            _totalLearnedCount++;
            UpdateDisplay();
        }

        /// <summary>
        /// 增加经验值
        /// </summary>
        public void AddXP(int amount)
        {
            _xp += amount;
            CheckLevelUp();
        }

        /// <summary>
        /// 检查是否满足升级条件并进行升级
        /// </summary>
        private void CheckLevelUp()
        {
            while (_xp >= _xpToNextLevel && _currentLevel < _levelTitles.Count - 1)
            {
                _xp -= _xpToNextLevel;
                _currentLevel++;
                _levelTitle = _levelTitles[_currentLevel];
                _xpToNextLevel = (_currentLevel + 1) * 100;

                _onLevelUp?.Invoke();
            }

            UpdateLevelDisplay();
        }

        /// <summary>
        /// 更新等级信息
        /// </summary>
        public void UpdateLevel()
        {
            _currentLevel = _xp / 100;
            _currentLevel = Math.Min(_currentLevel, _levelTitles.Count - 1);
            _levelTitle = _levelTitles[_currentLevel];
            _xpToNextLevel = (_currentLevel + 1) * 100;
            UpdateLevelDisplay();
        }

        /// <summary>
        /// 更新等级显示
        /// </summary>
        public void UpdateLevelDisplay()
        {
            if (_labelLevel != null)
            {
                _labelLevel.Text = $"🏅 {_levelTitle} Lv.{_currentLevel + 1}";
            }

            if (_progressXP != null)
            {
                _progressXP.Maximum = _xpToNextLevel;
                _progressXP.Value = Math.Min(_xp, _xpToNextLevel);
            }

            if (_labelXP != null)
            {
                _labelXP.Text = $"经验值: {_xp}/{_xpToNextLevel}";
            }
        }

        /// <summary>
        /// 更新所有统计显示
        /// </summary>
        public void UpdateDisplay()
        {
            if (_labelScore != null)
                _labelScore.Text = $"🏆 得分: {_totalScore}";

            if (_labelTodayCount != null)
                _labelTodayCount.Text = $"📚 今日学习: {_todayLearnedCount} 项";

            if (_labelStreak != null)
                _labelStreak.Text = $"🔥 连续学习: {_streakDays} 天";

            UpdateLevelDisplay();
        }

        /// <summary>
        /// 重置今日学习数（每日首次启动时调用）
        /// </summary>
        public void ResetTodayCount()
        {
            if (_lastStudyDate.Date != DateTime.Today)
            {
                _todayLearnedCount = 0;
            }
        }

        // 属性访问
        public int TodayLearnedCount => _todayLearnedCount;
        public int StreakDays => _streakDays;
        public int TotalScore => _totalScore;
        public int TotalLearnedCount => _totalLearnedCount;
        public int XP => _xp;
        public int CurrentLevel => _currentLevel + 1;
        public int XPToNextLevel => _xpToNextLevel;
        public string LevelTitle => _levelTitle;
    }
}
