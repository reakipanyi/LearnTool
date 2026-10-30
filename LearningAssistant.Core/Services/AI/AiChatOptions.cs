namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 单次 AI 请求参数：让 PDF 等专用场景按功能定制输出长度、温度与角色，
    /// 不影响 IAIService 四个旧方法的行为。
    /// </summary>
    public sealed record AiChatOptions
    {
        /// <summary>本次请求的 system prompt；为空时沿用各 Provider 的默认角色</summary>
        public string? SystemPrompt { get; init; }

        /// <summary>输出 token 上限</summary>
        public int MaxTokens { get; init; } = 1000;

        /// <summary>采样温度</summary>
        public double Temperature { get; init; } = 0.7;

        /// <summary>是否要求结构化 JSON 输出</summary>
        public bool JsonMode { get; init; }

        /// <summary>本次请求超时；为空时不额外限制</summary>
        public TimeSpan? Timeout { get; init; }

        /// <summary>临时覆盖模型名（不影响缓存键使用的 ModelId）</summary>
        public string? ModelOverride { get; init; }
    }

    /// <summary>
    /// 支持按次参数与流式输出的 AI 服务。
    /// 旧功能（词语解释、练习、摘要）仍走 IAIService，避免行为变化。
    /// </summary>
    public interface IAiChatCompletionService
    {
        /// <summary>一次性返回完整回答（不经过通用 AI 缓存）</summary>
        Task<string> CompleteAsync(string prompt, AiChatOptions options, CancellationToken cancellationToken = default);

        /// <summary>流式返回增量文本；供应商不支持时退化为一次性返回</summary>
        IAsyncEnumerable<string> StreamAsync(string prompt, AiChatOptions options, CancellationToken cancellationToken = default);

        /// <summary>实际请求的模型名，用于缓存键</summary>
        string ModelId { get; }

        /// <summary>供应商标识，用于缓存键</summary>
        string ProviderId { get; }
    }
}
