using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfStudyIntegration : IPdfStudyIntegration
    {
        private readonly ILogger<PdfStudyIntegration> _logger;
        private readonly IStudyEngine _studyEngine;

        private string _currentUserId = "Default";
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
                
                SubjectSubCategoryMapping.TryParseSubject(_currentLanguage, out var subject);
                SubjectSubCategoryMapping.TryParseSubCategory(_currentSubCategory, out var subCategory);
                var context = new LearningContext(_currentUserId, subject, subCategory);
                _studyEngine.Initialize(context);
                
                if (IsWordAlreadyExists(cleanWord))
                {
                    _logger.LogInformation("Word already exists in learning list: {Word}", cleanWord);
                    return false;
                }
                
                _studyEngine.AddUnknownItem(cleanWord, subCategory);
                
                WordAdded?.Invoke(this, new WordAddedEventArgs
                {
                    Word = cleanWord,
                    Language = _currentLanguage
                });
                
                _logger.LogInformation("Added word to learning list: {Word}", cleanWord);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add word to learning list: {Word}", word);
                return false;
            }
        }

        private bool IsWordAlreadyExists(string word)
        {
            try
            {
                var allItems = _studyEngine.GetAllItems();
                if (allItems.Any(item => 
                    string.Equals(item.GetMainContent().Trim(), word.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                var knownItems = _studyEngine.KnownItems;
                if (knownItems.Any(item => 
                    string.Equals(item.Trim(), word.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                var unknownItems = _studyEngine.UnknownItems;
                if (unknownItems.Any(item => 
                    string.Equals(item.Trim(), word.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if word exists: {Word}", word);
                return false;
            }
        }
    }
}