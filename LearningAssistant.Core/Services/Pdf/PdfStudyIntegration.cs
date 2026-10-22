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
        private readonly IStudyEntryService? _studyEntryService;

        private string _currentUserId = Constants.DefaultUserId;
        private string _currentLanguage = Constants.Language.English;
        private string _currentSubCategory = Constants.SubCategory.EnglishWord;

        public event EventHandler<WordAddedEventArgs>? WordAdded;

        public PdfStudyIntegration(
            ILogger<PdfStudyIntegration> logger,
            IStudyEngine studyEngine,
            IStudyEntryService? studyEntryService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
            _studyEntryService = studyEntryService;
        }

        public void SetCurrentUserAndConfig(string userId, string language, string subCategory)
        {
            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;
        }

        /// <summary>添加单词到学习列表；统一走 IStudyEntryService 写入个人学习库。</summary>
        [Obsolete("Use IStudyEntryService directly")]
        public bool AddWordToLearningList(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            if (_studyEntryService == null)
            {
                _logger.LogWarning("Study entry service is not available, cannot add word: {Word}", word);
                return false;
            }

            try
            {
                string cleanWord = word.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();

                SubjectSubCategoryMapping.TryParseSubject(_currentLanguage, out var subject);
                SubjectSubCategoryMapping.TryParseSubCategory(_currentSubCategory, out var subCategory);

                var result = _studyEntryService.AddAsync(new StudyEntryRequest
                {
                    Text = cleanWord,
                    Subject = subject,
                    SubCategory = subCategory,
                    SourceType = "PdfWord"
                }).GetAwaiter().GetResult();

                if (result.Item == null)
                {
                    return false;
                }

                if (result.Created)
                {
                    WordAdded?.Invoke(this, new WordAddedEventArgs
                    {
                        Word = cleanWord,
                        Language = _currentLanguage
                    });

                    _logger.LogInformation("Added word to learning list: {Word}", cleanWord);
                }
                else
                {
                    _logger.LogInformation("Word already exists in learning list: {Word}", cleanWord);
                }

                return result.Created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add word to learning list: {Word}", word);
                return false;
            }
        }
    }
}