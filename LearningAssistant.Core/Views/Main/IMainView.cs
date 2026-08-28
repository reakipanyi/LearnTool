namespace LearningAssistant.Views
{
    /// <summary>
    /// 主视图接口 - 提供主窗口的显示和交互功能
    /// </summary>
    public interface IMainView
    {
        /// <summary>
        /// 当前选中的用户名
        /// </summary>
        string SelectedUser { get; set; }

        /// <summary>
        /// 学习进度摘要文本
        /// </summary>
        string ProgressSummary { get; set; }

        /// <summary>
        /// 状态栏文本
        /// </summary>
        string StatusText { get; set; }

        /// <summary>
        /// 用户切换事件
        /// </summary>
        event EventHandler? UserChanged;

        /// <summary>
        /// 打开学习窗口点击事件
        /// </summary>
        event EventHandler? OpenLearningWindowClicked;

        /// <summary>
        /// 打开设置点击事件
        /// </summary>
        event EventHandler? OpenSettingsClicked;

        /// <summary>
        /// 打开编辑器点击事件
        /// </summary>
        event EventHandler? OpenEditorClicked;

        /// <summary>
        /// 标签页切换事件
        /// </summary>
        event EventHandler? TabChanged;

        /// <summary>
        /// 用户对比(PK)点击事件
        /// </summary>
        event EventHandler? OpenUserComparisonClicked;

        /// <summary>
        /// 显示消息对话框
        /// </summary>
        /// <param name="msg">消息内容</param>
        void ShowMessage(string msg);

        /// <summary>
        /// 刷新用户列表
        /// </summary>
        /// <param name="users">用户ID列表</param>
        void RefreshUserList(IEnumerable<string> users);

        /// <summary>
        /// 更新状态文本
        /// </summary>
        /// <param name="status">状态文本</param>
        void UpdateStatus(string status);

        /// <summary>
        /// 更新连续学习天数信息
        /// </summary>
        /// <param name="consecutiveDays">连续学习天数</param>
        /// <param name="studyTimeSummary">学习时间摘要</param>
        void UpdateStreakInfo(int consecutiveDays, string studyTimeSummary);

        /// <summary>
        /// 更新 Dashboard 统计卡片数据
        /// </summary>
        /// <param name="todayStudyMinutes">今日学习分钟数</param>
        /// <param name="streakDays">连续学习天数</param>
        /// <param name="totalXP">总经验值</param>
        /// <param name="currentLevel">当前等级</param>
        /// <param name="xpToNextLevel">距下一级所需XP</param>
        /// <param name="completedChallenges">已完成挑战数</param>
        /// <param name="totalChallenges">总挑战数</param>
        /// <param name="noteCount">笔记总数</param>
        /// <param name="todayNewNotes">今日新增笔记数</param>
        void UpdateDashboardStats(int todayStudyMinutes, int streakDays, int totalXP,
            int currentLevel, int xpToNextLevel, int completedChallenges, int totalChallenges,
            int noteCount = 0, int todayNewNotes = 0);

        void UpdateRecommendations(List<LearningAssistant.Models.Learning.LearningRecommendation> recommendations);

    }

    /// <summary>
    /// 用户对比数据 - 用于PK界面展示
    /// </summary>
    public class UserComparisonData
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 连续学习天数
        /// </summary>
        public int ConsecutiveStudyDays { get; set; }

        /// <summary>
        /// 今日学习时长（分钟）
        /// </summary>
        public int TodayStudyTimeMinutes { get; set; }

        /// <summary>
        /// 正确率（百分比）
        /// </summary>
        public double AccuracyRate { get; set; }

        /// <summary>
        /// 已掌握词汇数
        /// </summary>
        public int KnownItemsCount { get; set; }

        /// <summary>
        /// 累计学习时长（分钟）
        /// </summary>
        public int TotalStudyTimeMinutes { get; set; }

        /// <summary>
        /// 总词汇量
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 成就徽章数
        /// </summary>
        public int AchievementCount { get; set; }
    }
}
