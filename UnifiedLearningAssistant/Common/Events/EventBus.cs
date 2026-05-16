
using System.Collections.Concurrent;

namespace UnifiedLearningAssistant.Common.Events
{
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary&lt;Type, List&lt;Delegate&gt;&gt; _handlers = new ConcurrentDictionary&lt;Type, List&lt;Delegate&gt;&gt;();

        public void Subscribe&lt;TEvent&gt;(Action&lt;TEvent&gt; handler) where TEvent : IApplicationEvent
        {
            var eventType = typeof(TEvent);
            var handlers = _handlers.GetOrAdd(eventType, _ =&gt; new List&lt;Delegate&gt;());
            lock (handlers)
            {
                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        public void Unsubscribe&lt;TEvent&gt;(Action&lt;TEvent&gt; handler) where TEvent : IApplicationEvent
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

        public void Publish&lt;TEvent&gt;(TEvent eventData) where TEvent : IApplicationEvent
        {
            var eventType = typeof(TEvent);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                List&lt;Delegate&gt; currentHandlers;
                lock (handlers)
                {
                    currentHandlers = new List&lt;Delegate&gt;(handlers);
                }

                foreach (var handler in currentHandlers)
                {
                    if (handler is Action&lt;TEvent&gt; typedHandler)
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

