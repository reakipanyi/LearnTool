using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    public interface ILearningGoalService
    {
        void SetDailyGoal(string userId, int itemsPerDay);
        DailyGoal? GetDailyGoal(string userId);
        int GetTodayProgress(string userId);
        bool IsDailyGoalCompleted(string userId);
        List<DailyGoal> GetGoalHistory(string userId, int days = 30);
    }
}
