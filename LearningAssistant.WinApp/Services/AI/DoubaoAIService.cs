using LearningAssistant.Models.Config;
using LearningAssistant.Services.Cache;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 豆包 (Doubao) AI 服务 - 复用 OpenAICompatibleAIService 的通用实现，
    /// 仅保留豆包特有的解释 Prompt 文案。
    /// </summary>
    public class DoubaoAIService : OpenAICompatibleAIService
    {
        public DoubaoAIService(
            AiConfig config,
            ICacheService cacheService,
            ILogger<DoubaoAIService> logger,
            HttpClient httpClient,
            AiEndpoint? endpoint = null)
            : base(config, cacheService, logger, httpClient, "doubao", "Doubao", endpoint)
        {
        }

        protected override string BuildExplanationPrompt(string text, string language, string subType)
        {
            var typeName = subType switch
            {
                "识字" => "汉字",
                "成语" => "成语",
                "短语" => "短语",
                "诗词" => "诗词",
                "语文综合" => "语文综合内容",
                "英语单词" => "英语单词",
                "英语短语" => "英语短语",
                "英语句子" => "英语句子",
                "英语综合" => "英语综合内容",
                _ => "词语"
            };

            if (language == "中文")
            {
                if (subType == "语文综合")
                {
                    return $"请简要解析：{text}\n要求：控制在100字以内，简洁明了，突出重点。格式：概要+要点。";
                }
                return $"请简要解释{typeName}：{text}\n要求：控制在100字以内。格式：读音+含义+简单用法示例。";
            }
            else
            {
                if (subType == "英语综合")
                {
                    return $"请简要解析：{text}\n要求：控制在100字以内，简洁明了。格式：概要+要点+学习提示。";
                }
                return $"请简要解释{typeName}：{text}\n要求：控制在100字以内。格式：音标+中文释义+简单例句。";
            }
        }
    }
}
