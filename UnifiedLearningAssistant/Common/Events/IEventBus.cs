
namespace UnifiedLearningAssistant.Common.Events
{
    public interface IEventBus
    {
        void Subscribe&lt;TEvent&gt;(Action&lt;TEvent&gt; handler) where TEvent : IApplicationEvent;
        void Unsubscribe&lt;TEvent&gt;(Action&lt;TEvent&gt; handler) where TEvent : IApplicationEvent;
        void Publish&lt;TEvent&gt;(TEvent eventData) where TEvent : IApplicationEvent;
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

