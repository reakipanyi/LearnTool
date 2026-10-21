namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF AI 结果缓存：按文件指纹 + 页码集合 + 功能 + 语言 + 提示词版本 + 模型生成 Key，
    /// 磁盘持久化并按容量上限做 LRU 淘汰。
    /// </summary>
    public interface IPdfAiCacheService
    {
        /// <summary>当前缓存条目数</summary>
        int Count { get; }

        /// <summary>
        /// 生成缓存 Key（SHA256 截断），页码集合会去重排序。
        /// </summary>
        string BuildKey(
            string fingerprint,
            IEnumerable<int> pageIndexes,
            string feature,
            string language,
            string promptVersion,
            string model,
            string? extra = null);

        /// <summary>尝试读取缓存内容，命中时刷新 LRU 访问时间。</summary>
        bool TryGet(string key, out string content);

        /// <summary>写入缓存并触发容量淘汰。</summary>
        void Set(string key, string content);
    }
}
