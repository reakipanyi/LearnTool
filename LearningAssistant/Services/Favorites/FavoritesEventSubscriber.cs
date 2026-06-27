using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Services.Learning;
using KidWinApp.Services;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Favorites
{
    public class FavoritesEventSubscriber : IDisposable
    {
        private readonly ILogger<FavoritesEventSubscriber> _logger;
        private readonly IStudyEngine _studyEngine;
        private readonly IEventBus _eventBus;
        private bool _disposed = false;

        public FavoritesEventSubscriber(
            ILogger<FavoritesEventSubscriber> logger,
            IStudyEngine studyEngine,
            IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            SubscribeToEvents();
            _logger.LogInformation("FavoritesEventSubscriber initialized");
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<FavoriteAddedEvent>(OnFavoriteAdded);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _eventBus.Unsubscribe<FavoriteAddedEvent>(OnFavoriteAdded);
                _logger.LogInformation("FavoritesEventSubscriber disposed");
            }

            _disposed = true;
        }

        private void OnFavoriteAdded(FavoriteAddedEvent @event)
        {
            try
            {
                _logger.LogInformation("FavoritesEventSubscriber received FavoriteAddedEvent: {ItemContent}", @event.ItemContent);

                var cleanContent = @event.ItemContent?.Trim();
                if (string.IsNullOrWhiteSpace(cleanContent))
                {
                    _logger.LogWarning("Favorite content is empty, skipping");
                    return;
                }

                var allItems = _studyEngine.GetAllItems();
                if (allItems.Any(item =>
                    string.Equals(item.GetMainContent().Trim(), cleanContent, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Item already exists in study engine: {ItemContent}", cleanContent);
                    return;
                }

                var subCategory = InferSubCategory(cleanContent);
                _studyEngine.AddUnknownItem(cleanContent, subCategory);
                _logger.LogInformation("Added favorite item to study engine: {ItemContent}, SubCategory: {SubCategory}", cleanContent, subCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add favorite to study engine");
            }
        }

        private string InferSubCategory(string content)
        {
            var languageType = StringLanguageDetector.DetectLanguage(content);
            
            switch (languageType)
            {
                case LanguageType.Chinese:
                    if (content.Length == 1)
                        return Constants.SubCategory.ChineseCharacter;
                    else if (content.Length == 4 && IsIdiomPattern(content))
                        return Constants.SubCategory.ChineseIdiom;
                    else if (content.Length <= 8)
                        return Constants.SubCategory.ChinesePhrase;
                    else
                        return Constants.SubCategory.ChineseComprehensive;
                
                case LanguageType.English:
                    if (content.Contains(' '))
                        return Constants.SubCategory.EnglishPhrase;
                    else
                        return Constants.SubCategory.EnglishWord;
                
                case LanguageType.Mixed:
                    if (content.Length <= 10)
                        return Constants.SubCategory.EnglishPhrase;
                    else
                        return Constants.SubCategory.EnglishComprehensive;
                
                default:
                    return Constants.SubCategory.EnglishWord;
            }
        }

        private bool IsIdiomPattern(string content)
        {
            return content.Length == 4;
        }
    }
}