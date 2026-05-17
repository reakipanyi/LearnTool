
using System.Collections.Concurrent;

namespace UnifiedLearningAssistant.Common.Events
{
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new ConcurrentDictionary<Type, List<Delegate>>();

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent
        {
            var eventType = typeof(TEvent);
            var handlers = _handlers.GetOrAdd(eventType, _ => new List<Delegate>());
            lock (handlers)
            {
                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent
        {
            var eventType = typeof(TEvent);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                lock (handlers)
                {
                    handlers.Remove(handler);
                }
            }
        }

        public void Publish<TEvent>(TEvent eventData) where TEvent : IApplicationEvent
        {
            var eventType = typeof(TEvent);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                List<Delegate> currentHandlers;
                lock (handlers)
                {
                    currentHandlers = new List<Delegate>(handlers);
                }

                foreach (var handler in currentHandlers)
                {
                    if (handler is Action<TEvent> typedHandler)
                    {
                        try
                        {
                            typedHandler(eventData);
                        }
                        catch
                        {
                            // 可以在这里添加日志记录
                        }
                    }
                }
            }
        }
    }
}

