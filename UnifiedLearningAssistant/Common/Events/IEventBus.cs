
namespace UnifiedLearningAssistant.Common.Events
{
    public interface IEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent;
        void Publish<TEvent>(TEvent eventData) where TEvent : IApplicationEvent;
    }
    
    public interface IApplicationEvent
    {
        DateTime Timestamp { get; }
    }
    
    public abstract class ApplicationEventBase : IApplicationEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}

