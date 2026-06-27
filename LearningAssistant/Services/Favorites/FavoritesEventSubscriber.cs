using LearningAssistant.Common.Events;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Favorites
{
    public class FavoritesEventSubscriber
    {
        private readonly ILogger<FavoritesEventSubscriber> _logger;
        private readonly IStudyEngine _studyEngine;
        private readonly IEventBus _eventBus;

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
            _eventBus.Unsubscribe<FavoriteAddedEvent>(OnFavoriteAdded);
            _logger.LogInformation("FavoritesEventSubscriber disposed");
        }

        private void OnFavoriteAdded(FavoriteAddedEvent @event)
        {
            try
            {
                _logger.LogInformation("FavoritesEventSubscriber received FavoriteAddedEvent: {ItemContent}", @event.ItemContent);

                var allItems = _studyEngine.GetAllItems();
                if (allItems.Any(item =>
                    string.Equals(item.GetMainContent().Trim(), @event.ItemContent.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Item already exists in study engine: {ItemContent}", @event.ItemContent);
                    return;
                }

                _studyEngine.AddItem(@event.ItemContent, "收藏");
                _logger.LogInformation("Added favorite item to study engine: {ItemContent}", @event.ItemContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add favorite to study engine");
            }
        }
    }
}