namespace LearningAssistant.Services.Cache
{
    public interface ICacheService
    {
        bool TryGet<T>(string key, out T value);

        Task<T?> TryGetAsync<T>(string key);

        /// <summary>
        /// 设置缓存值
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationMinutes">过期时间（分钟），为null则永不过期</param>
        void Set<T>(string key, T value, int? expirationMinutes = null);

        /// <summary>
        /// 获取缓存值，若不存在则调用factory创建并缓存
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">当缓存不存在时的创建回调</param>
        /// <param name="expirationMinutes">过期时间（分钟）</param>
        /// <returns>缓存值</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? expirationMinutes = null);

        /// <summary>
        /// 批量设置缓存值
        /// </summary>
        /// <typeparam name="T">缓存对象类型</typeparam>
        /// <param name="items">缓存项字典，键为缓存键，值为缓存值</param>
        /// <param name="expirationMinutes">过期时间（分钟）</param>
        void SetMany<T>(IDictionary<string, T> items, int? expirationMinutes = null);

        /// <summary>
        /// 移除指定缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        void Remove(string key);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void Clear();

        /// <summary>
        /// 将内存缓存持久化到磁盘
        /// </summary>
        void Persist();

        /// <summary>
        /// 缓存预热 - 异步加载常用数据到缓存
        /// </summary>
        /// <param name="warmupTasks">预热任务字典，键为缓存键前缀，值为预热工厂函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预热完成的项数量</returns>
        Task<int> WarmupAsync(IDictionary<string, Func<Task>> warmupTasks, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存命中率统计
        /// </summary>
        (int Hits, int Misses) GetStatistics();

        /// <summary>
        /// 当前缓存项数量
        /// </summary>
        int Count { get; }
    }
}
