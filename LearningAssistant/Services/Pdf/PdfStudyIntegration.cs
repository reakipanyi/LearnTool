using LearningAssistant.Common;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfStudyIntegration : IPdfStudyIntegration
    {
        private readonly ILogger<PdfStudyIntegration> _logger;
        private readonly IStudyEngine _studyEngine;

        private string _currentUserId = "Guest";
        private string _currentLanguage = Constants.Language.English;
        private string _currentSubCategory = Constants.SubCategory.EnglishWord;

        public event EventHandler<WordAddedEventArgs>? WordAdded;

        public PdfStudyIntegration(ILogger<PdfStudyIntegration> logger, IStudyEngine studyEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
        }

        public void SetCurrentUserAndConfig(string userId, string language, string subCategory)
        {
            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;
        }

        public bool AddWordToLearningList(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            try
            {
                string cleanWord = word.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                _studyEngine.Initialize(_currentUserId, _currentLanguage, _currentSubCategory, "", "", "");
                _studyEngine.AddUnknownItem(cleanWord, _currentSubCategory);
                
                WordAdded?.Invoke(this, new WordAddedEventArgs
                {
                    Word = cleanWord,
                    Language = _currentLanguage
                });
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add word to learning list: {Word}", word);
                return false;
            }
        }
    }
}