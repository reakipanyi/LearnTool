using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Learning
{
    public interface ILearningSessionRepository
    {
        void SaveSession(LearningSession session);
        LearningSession? GetSession(string sessionId);
        List<LearningSession> GetUserSessions(string userId, int limit = 50);
        List<LearningSession> GetSessionsByDate(string userId, DateTime startDate, DateTime endDate);
        void DeleteSession(string sessionId);
    }
}
