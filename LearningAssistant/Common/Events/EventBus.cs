using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Common.Events
{
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new ConcurrentDictionary<Type, List<Delegate>>();
        private readonly ILogger<EventBus>? _logger;

        public EventBus(ILogger<EventBus>? logger = null)
        {
            _logger = logger;
        }

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

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
            var snapshot = GetHandlerSnapshot<TEvent>();
            var eventType = typeof(TEvent);

            foreach (var handler in snapshot)
            {
                if (handler is Action<TEvent> typedHandler)
                {
                    try
                    {
                        typedHandler(eventData);
                    }
                    catch (Exception ex)
                    {
                        // 记录异常但不中断其他处理器
                        _logger?.LogError(ex, "事件处理器执行失败: 事件类型 {EventType}, 处理器 {Handler}",
                            eventType.Name, typedHandler.Method.Name);
                    }
                }
            }
        }

        public Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IApplicationEvent
        {
            var snapshot = GetHandlerSnapshot<TEvent>();
            var eventType = typeof(TEvent);

            // 将同步处理器包装为异步执行，避免阻塞调用线程
            return Task.Run(() =>
            {
                foreach (var handler in snapshot)
                {
                    if (handler is Action<TEvent> typedHandler)
                    {
                        try
                        {
                            typedHandler(eventData);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "异步事件处理器执行失败: 事件类型 {EventType}, 处理器 {Handler}",
                                eventType.Name, typedHandler.Method.Name);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 获取处理器快照，避免在调用过程中订阅列表发生变化
        /// </summary>
        private List<Delegate> GetHandlerSnapshot<TEvent>() where TEvent : IApplicationEvent
        {
            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                return new List<Delegate>();
            }

            lock (handlers)
            {
                return new List<Delegate>(handlers);
            }
        }
    }
}
