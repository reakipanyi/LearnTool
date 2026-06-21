using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 学习目标服务接口
    /// 提供目标设置、进度追踪、连续达成统计等功能
    /// </summary>
    public interface ILearningGoalService
    {
        /// <summary>
        /// 设置每日学习数量目标（兼容旧接口）
        /// </summary>
        void SetDailyGoal(string userId, int itemsPerDay);

        /// <summary>
        /// 获取每日目标（兼容旧接口）
        /// </summary>
        DailyGoal? GetDailyGoal(string userId);

        /// <summary>
        /// 获取今日进度百分比（兼容旧接口）
        /// </summary>
        int GetTodayProgress(string userId);

        /// <summary>
        /// 检查每日目标是否完成（兼容旧接口）
        /// </summary>
        bool IsDailyGoalCompleted(string userId);

        /// <summary>
        /// 获取目标历史记录（兼容旧接口）
        /// </summary>
        List<DailyGoal> GetGoalHistory(string userId, int days = 30);

        /// <summary>
        /// 获取所有启用的目标设置
        /// </summary>
        List<LearningGoal> GetGoals(string userId);

        /// <summary>
        /// 更新目标设置
        /// </summary>
        void UpdateGoal(string userId, LearningGoal goal);

        /// <summary>
        /// 启用/禁用目标
        /// </summary>
        void SetGoalEnabled(string userId, GoalType type, bool enabled);

        /// <summary>
        /// 获取今日所有目标进度
        /// </summary>
        List<GoalProgress> GetTodayProgressList(string userId);

        /// <summary>
        /// 更新今日学习时长进度
        /// </summary>
        void UpdateStudyMinutes(string userId, int minutes);

        /// <summary>
        /// 增加今日学习数量
        /// </summary>
        void IncrementStudyItems(string userId, int count = 1);

        /// <summary>
        /// 增加今日复习数量
        /// </summary>
        void IncrementReviewItems(string userId, int count = 1);

        /// <summary>
        /// 检查所有目标完成情况，触发达成事件
        /// </summary>
        void CheckGoalCompletion(string userId);

        /// <summary>
        /// 获取每日目标完成记录
        /// </summary>
        List<DailyGoalRecord> GetDailyRecords(string userId, int days = 30);

        /// <summary>
        /// 获取连续达成统计
        /// </summary>
        StreakInfo GetStreakInfo(string userId);

        /// <summary>
        /// 目标达成事件
        /// </summary>
        event EventHandler<GoalType>? GoalCompleted;

        /// <summary>
        /// 所有目标达成事件
        /// </summary>
        event EventHandler? AllGoalsCompleted;
    }
}
