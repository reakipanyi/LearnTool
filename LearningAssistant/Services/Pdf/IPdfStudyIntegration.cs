namespace LearningAssistant.Services.Pdf
{
    public interface IPdfStudyIntegration
    {
        void SetCurrentUserAndConfig(string userId, string language, string subCategory);
        bool AddWordToLearningList(string word);
        event EventHandler<WordAddedEventArgs>? WordAdded;
    }

    public class WordAddedEventArgs : EventArgs
    {
        public string Word { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}