namespace UnifiedLearningAssistant.Services.Feedback
{
    public interface ISoundService
    {
        void PlaySuccess();
        void PlayError();
        void PlayNavigation();
        void PlayAchievement();
        void PlayClick();
    }
}
