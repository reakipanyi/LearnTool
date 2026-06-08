namespace LearningAssistant.Services.Feedback
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
