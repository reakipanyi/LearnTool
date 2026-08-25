
namespace LearningAssistant.Common.Events
{
    /// <summary>
    /// 事件总线接口 - 提供应用内事件的发布订阅机制
    /// 用于解耦组件间的通信
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理回调</param>
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent;

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">之前订阅的事件处理回调</param>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent;

        /// <summary>
        /// 同步发布事件
        /// 所有订阅了该事件类型的处理器都会被依次调用
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<TEvent>(TEvent eventData) where TEvent : IApplicationEvent;

        /// <summary>
        /// 异步发布事件 - 不会阻塞调用线程
        /// 处理器在后台线程上依次执行
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        Task PublishAsync<TEvent>(TEvent eventData) where TEvent : IApplicationEvent;
    }

    /// <summary>
    /// 应用事件接口 - 所有应用事件需实现此接口
    /// </summary>
    public interface IApplicationEvent
    {
        /// <summary>
        /// 事件发生的时间戳（UTC时间）
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// 应用事件基类 - 提供时间戳的默认实现
    /// </summary>
    public abstract class ApplicationEventBase : IApplicationEvent
    {
        /// <summary>
        /// 事件时间戳，默认为UTC当前时间
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
