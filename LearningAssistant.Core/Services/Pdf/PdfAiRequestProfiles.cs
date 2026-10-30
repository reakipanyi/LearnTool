using LearningAssistant.Services.AI;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF AI 各功能的请求参数与提示词版本。
    /// 调整提示词或参数时递增对应版本号，可让旧缓存自然失效。
    /// </summary>
    public static class PdfAiRequestProfiles
    {
        public const string Summarize = "summarize";
        public const string Ask = "ask";
        public const string AskRetrieval = "ask-retrieval";
        public const string AskScoped = "ask-scoped";
        public const string Explain = "explain";
        public const string Outline = "outline";
        public const string Notes = "notes";
        public const string Flashcards = "flashcards";
        public const string AnnotationDigest = "annotation-digest";
        public const string SummaryEntries = "summary-entries";
        public const string TopicCluster = "topic-cluster";
        public const string Quiz = "quiz";

        /// <summary>分块 map-reduce 的块摘要功能名（单独缓存）</summary>
        public const string ChunkSummary = "chunk-summary";

        /// <summary>提示词版本，参数或文案变化时递增</summary>
        private const string PromptVersion = "pdf-ai-v2";

        /// <summary>按功能返回请求参数：输出长度、温度、JSON 模式与角色设定</summary>
        public static AiChatOptions Get(string feature)
        {
            var (maxTokens, temperature, jsonMode, systemPrompt) = feature switch
            {
                Summarize => (1200, 0.2, false,
                    "你是严谨的文档摘要助手，只依据用户提供的文档内容作答，不编造信息。"),
                Ask => (1500, 0.3, false,
                    "你是文档问答助手，只依据给定页面作答；依据不足时必须明确说明未在文档中找到依据。"),
                AskRetrieval => (1500, 0.3, false,
                    "你是文档问答助手，只依据给定页面作答；依据不足时必须明确说明未在文档中找到依据。"),
                AskScoped => (1500, 0.3, false,
                    "你是文档问答助手，只依据给定页面作答；依据不足时必须明确说明未在文档中找到依据。"),
                Explain => (1000, 0.4, false,
                    "你是阅读辅导助手，结合页面上下文解释选中内容，不要重复粘贴原文。"),
                Outline => (2500, 0.1, true,
                    "你是文档结构分析助手，只输出严格合法的 JSON，不输出任何解释文字。"),
                Notes => (3500, 0.2, false,
                    "你是学习笔记整理助手，输出结构化 Markdown，并为关键内容保留页码锚点。"),
                Flashcards => (2500, 0.1, true,
                    "你是闪卡生成助手，只输出严格合法的 JSON 数组，每张卡聚焦一个知识点。"),
                AnnotationDigest => (2000, 0.2, false,
                    "你是学习笔记整理助手，把用户标注整理成要点、待办、疑问三类。"),
                SummaryEntries => (1500, 0.1, true,
                    "你是学习摘要助手，只输出严格合法的 JSON 数组。"),
                TopicCluster => (2500, 0.1, true,
                    "你是主题聚类助手，只输出严格合法的 JSON 数组。"),
                Quiz => (2000, 0.4, false,
                    "你是出题助手，只依据给定内容出题，题目与答案必须能在原文中找到依据。"),
                ChunkSummary => (800, 0.1, false,
                    "你是文档分段摘要助手，只依据给定页面归纳要点，并保留页码锚点。"),
                _ => (1200, 0.3, false, null as string)
            };

            return new AiChatOptions
            {
                MaxTokens = maxTokens,
                Temperature = temperature,
                JsonMode = jsonMode,
                SystemPrompt = systemPrompt
            };
        }

        /// <summary>该功能的缓存键提示词版本</summary>
        public static string GetVersion(string feature) => $"{PromptVersion}-{feature}";
    }
}
