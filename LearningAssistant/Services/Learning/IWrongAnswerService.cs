using LearningAssistant.Common;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning
{
    public interface IWrongAnswerService
    {
        void AddWrongAnswer(string userId, WrongAnswerItem item);
        void RemoveWrongAnswer(string userId, string itemId);
        List<WrongAnswerItem> GetWrongAnswers(string userId, SubjectType? subject = null, SubCategoryType? category = null);
        List<WrongAnswerItem> GetBySubjectCategory(string userId, SubjectType subject, SubCategoryType category);
        List<WrongAnswerItem> GetWrongAnswersForReview(string userId, int count = 10);
        void MarkAsReviewed(string userId, string itemId, bool remembered);
        void MarkAsMastered(string userId, string itemId);
        int GetWrongAnswerCount(string userId);
        int GetMasteredCount(string userId);
        void ExportWrongAnswers(string userId, string filePath);
        List<WrongAnswerItem> GetWrongAnswers(string userId, WrongAnswerFilter filter);
        List<WrongAnswerItem> GetWrongAnswers(string userId, int skip, int take);
        void UpdateMastery(string userId, string itemId, MasteryLevel mastery);
        List<WrongAnswerItem> SearchWrongAnswers(string userId, string keyword);
        WrongAnswerStats GetStatistics(string userId);
        List<SubjectType> GetSubjects(string userId);
        List<SubCategoryType> GetCategories(string userId, SubjectType subject);
        Dictionary<string, int> GetAllTags(string userId);
        void AddTag(string userId, string itemId, string tag);
        void RemoveTag(string userId, string itemId, string tag);
        void BatchUpdateMastery(string userId, List<string> itemIds, MasteryLevel mastery);
        void BatchRemove(string userId, List<string> itemIds);
        bool ExportToMarkdown(string userId, string filePath, WrongAnswerFilter? filter = null);
        bool ExportToTextCards(string userId, string filePath, WrongAnswerFilter? filter = null);
        (List<WrongAnswerItem> items, int total) GetWrongAnswersPaged(
            string userId, WrongAnswerFilter filter, int page = 1, int pageSize = 20);
    }
}
