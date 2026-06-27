using LearningAssistant.Common.Events;
using LearningAssistant.Services.KnowledgeGraph;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.KnowledgeGraph
{
    public class KnowledgeGraphEventSubscriber : IDisposable
    {
        private readonly ILogger<KnowledgeGraphEventSubscriber> _logger;
        private readonly IKnowledgeGraphService _knowledgeGraphService;
        private readonly IEventBus _eventBus;
        private bool _disposed = false;

        public KnowledgeGraphEventSubscriber(
            ILogger<KnowledgeGraphEventSubscriber> logger,
            IKnowledgeGraphService knowledgeGraphService,
            IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _knowledgeGraphService = knowledgeGraphService ?? throw new ArgumentNullException(nameof(knowledgeGraphService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            SubscribeToEvents();
            _logger.LogInformation("KnowledgeGraphEventSubscriber initialized");
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<ItemLearnedEvent>(OnItemLearned);
            _eventBus.Subscribe<ItemWrongEvent>(OnItemWrong);
            _eventBus.Subscribe<PDFHighlightEvent>(OnPdfHighlight);
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
                _eventBus.Unsubscribe<ItemLearnedEvent>(OnItemLearned);
                _eventBus.Unsubscribe<ItemWrongEvent>(OnItemWrong);
                _eventBus.Unsubscribe<PDFHighlightEvent>(OnPdfHighlight);
                _logger.LogInformation("KnowledgeGraphEventSubscriber disposed");
            }

            _disposed = true;
        }

        private async void OnItemLearned(ItemLearnedEvent @event)
        {
            try
            {
                _logger.LogInformation("KnowledgeGraph received ItemLearnedEvent: {ItemContent}", @event.ItemContent);

                var node = await _knowledgeGraphService.AddNodeAsync(
                    @event.UserId,
                    @event.ItemContent,
                    @event.SubCategory);

                await _knowledgeGraphService.UpdateMasteryAsync(@event.UserId, node.Id, 0.9);

                _logger.LogInformation("Updated mastery to 0.9 for node: {NodeId}", node.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update knowledge graph on ItemLearned");
            }
        }

        private async void OnItemWrong(ItemWrongEvent @event)
        {
            try
            {
                _logger.LogInformation("KnowledgeGraph received ItemWrongEvent: {ItemContent}", @event.ItemContent);

                var node = await _knowledgeGraphService.AddNodeAsync(
                    @event.UserId,
                    @event.ItemContent,
                    @event.SubCategory);

                var currentMastery = Math.Max(0.1, node.MasteryLevel - 0.2);
                await _knowledgeGraphService.UpdateMasteryAsync(@event.UserId, node.Id, currentMastery);

                _logger.LogInformation("Updated mastery to {Mastery} for node: {NodeId}", currentMastery, node.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update knowledge graph on ItemWrong");
            }
        }

        private async void OnPdfHighlight(PDFHighlightEvent @event)
        {
            try
            {
                _logger.LogInformation("KnowledgeGraph received PDFHighlightEvent: {HighlightedText}", @event.HighlightedText);

                var node = await _knowledgeGraphService.AddNodeAsync(
                    @event.UserId,
                    @event.HighlightedText,
                    @event.SelectedCategory ?? "PDF");

                await _knowledgeGraphService.UpdateMasteryAsync(@event.UserId, node.Id, 0.3);

                _logger.LogInformation("Added PDF highlight node: {NodeId}", node.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update knowledge graph on PDFHighlight");
            }
        }
    }
}