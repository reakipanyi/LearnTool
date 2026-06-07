namespace LearningAssistant.Services.Cache
{
    public interface ICacheService
    {
        bool TryGet<T>(string key, out T value);
        void Set<T>(string key, T value, int? expirationMinutes = null);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? expirationMinutes = null);
        void Remove(string key);
        void Clear();
        void Persist();
        int Count { get; }
    }
}