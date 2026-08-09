using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI提示词服务 - 从JSON配置加载提示词模板
    /// </summary>
    public class AIPromptService
    {
        private AIPromptConfig _config;
        private readonly ILogger<AIPromptService>? _logger;
        private readonly object _lock = new object();

        public AIPromptService(ILogger<AIPromptService>? logger = null)
        {
            _logger = logger;
            _config = LoadConfig();
        }

        private AIPromptConfig LoadConfig()
        {
            try
            {
                // 尝试从多个位置加载配置
                string[] configPaths = new[]
                {
                    AppPaths.AiPromptsPath
                };

                foreach (var configPath in configPaths)
                {
                    if (File.Exists(configPath))
                    {
                        var json = File.ReadAllText(configPath);
                        var config = JsonSerializer.Deserialize<AIPromptConfig>(json);
                        if (config != null)
                        {
                            _logger?.LogInformation("成功加载AI提示词配置: {Path}", configPath);
                            return config;
                        }
                    }
                }

                _logger?.LogWarning("未找到AI提示词配置文件，使用默认配置");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载AI提示词配置失败");
            }

            return new AIPromptConfig();
        }

        /// <summary>
        /// 获取解释提示词
        /// </summary>
        public string GetExplanationPrompt(string text, string language, string subType)
        {
            try
            {
                var prompts = language == "中文" ? _config.Prompts.Explanation.Chinese
                                                    : _config.Prompts.Explanation.English;

                if (prompts.TryGetValue(subType, out var template))
                {
                    return template.Replace("{text}", text);
                }

                // 默认提示词
                return $"请简要解释：{text}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取解释提示词失败");
                return $"请简要解释：{text}";
            }
        }

        /// <summary>
        /// 获取快捷操作提示词
        /// </summary>
        public string GetQuickActionPrompt(string action, string context)
        {
            try
            {
                if (_config.Prompts.QuickActions.TryGetValue(action, out var template))
                {
                    return $"{template}\n{context}";
                }
                return context;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取快捷操作提示词失败");
                return context;
            }
        }

        /// <summary>
        /// 获取练习生成提示词
        /// </summary>
        public string GetExercisePrompt(string text, string language)
        {
            try
            {
                var prompts = language == "中文" ? _config.Prompts.Exercise.Chinese
                                                   : _config.Prompts.Exercise.English;
                return prompts.Replace("{text}", text);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取练习提示词失败");
                return $"请针对以下内容生成练习题：\n\n{text}";
            }
        }

        /// <summary>
        /// 获取总结提示词
        /// </summary>
        public string GetSummarizePrompt(string text)
        {
            try
            {
                return _config.Prompts.Summarize.Replace("{text}", text);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取总结提示词失败");
                return $"请简要总结以下文本的主要内容：\n\n{text}";
            }
        }

        /// <summary>
        /// 获取问答提示词
        /// </summary>
        public string GetQAPrompt(string question, string context)
        {
            try
            {
                var prompt = _config.Prompts.Qa
                    .Replace("{context}", context)
                    .Replace("{question}", question);
                return prompt;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取问答提示词失败");
                return $"Context: {context}\n\nQuestion: {question}";
            }
        }

        /// <summary>
        /// 获取系统提示词
        /// </summary>
        public string GetSystemPrompt(string key = "default")
        {
            try
            {
                if (_config.SystemPrompts.TryGetValue(key, out var prompt))
                {
                    return prompt;
                }
                return _config.SystemPrompts.GetValueOrDefault("default",
                    "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取系统提示词失败");
                return "你是一个专业的语言学习助手，请用简洁明了的方式解释词语和回答问题。";
            }
        }

        /// <summary>
        /// 获取所有快捷操作类型
        /// </summary>
        public string[] GetQuickActionTypes()
        {
            var types = new string[_config.Prompts.QuickActions.Count];
            _config.Prompts.QuickActions.Keys.CopyTo(types, 0);
            return types;
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfig()
        {
            lock (_lock)
            {
                _config = LoadConfig();
            }
        }
    }
}
