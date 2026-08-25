using System.Collections.Immutable;

namespace LearningAssistant.Models.Config
{
    public class AiConfig
    {
        private string _provider = "doubao";

        public string Provider
        {
            get => _provider;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _provider = "doubao";
                    return;
                }

                var normalizedValue = value.Trim().ToLowerInvariant();
                if (Providers.ContainsKey(normalizedValue))
                {
                    _provider = normalizedValue;
                    return;
                }

                var matchedKey = Providers.Keys.FirstOrDefault(k =>
                    k.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase) ||
                    Providers[k].Name.Contains(value, StringComparison.OrdinalIgnoreCase));

                if (matchedKey != null)
                {
                    _provider = matchedKey;
                    return;
                }

                _provider = "doubao";
            }
        }

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 各 AI 提供商的独立配置（可选覆盖），key 为 provider 名（如 "deepseek"）。
        /// 用于为不同 provider 分别配置 API Key / 模型 / 地址，使 fallback 切换真正生效。
        /// </summary>
        public Dictionary<string, AiProviderInfo> ProviderConfigs { get; set; } = new();

        /// <summary>
        /// 解析指定 provider 实际使用的端点配置。
        /// 优先级：per-provider 覆盖配置 → 内置默认值（AiProviderInfo）→ 全局配置（仅对当前 provider 生效）。
        /// </summary>
        public AiEndpoint ResolveEndpoint(string provider)
        {
            var key = provider?.Trim().ToLowerInvariant() ?? string.Empty;

            Providers.TryGetValue(key, out var defaults);
            ProviderConfigs.TryGetValue(key, out var user);

            var baseUrl = FirstNonEmpty(user?.BaseUrl, defaults?.BaseUrl, BaseUrl);
            var model = FirstNonEmpty(user?.Model, defaults?.DefaultModel, Model);
            // 全局 ApiKey 仅对当前 provider 生效；其余 provider 需通过 ProviderConfigs 单独配置
            var apiKey = FirstNonEmpty(user?.ApiKey, key == Provider ? ApiKey : null, string.Empty);

            return new AiEndpoint(baseUrl, model, apiKey);
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v!.Trim();
            }
            return string.Empty;
        }

        public static readonly ImmutableDictionary<string, AiProviderInfo> Providers = new Dictionary<string, AiProviderInfo>()
        {
            {
                "doubao", new AiProviderInfo
                {
                    Name = "豆包 (Doubao)",
                    BaseUrl = "https://ark.cn-beijing.volces.com/api/v3/chat/completions",
                    DefaultModel = "doubao-pro-32k",
                    Models = new List<string>
                    {
                        "doubao-pro-32k",
                        "doubao-pro-128k",
                        "doubao-lite-32k",
                        "doubao-lite-128k"
                    },
                    WebViewUrl = "https://www.doubao.com/chat"
                }
            },
            {
                "deepseek", new AiProviderInfo
                {
                    Name = "DeepSeek",
                    BaseUrl = "https://api.deepseek.com/v1/chat/completions",
                    DefaultModel = "deepseek-chat",
                    Models = new List<string>
                    {
                        "deepseek-chat",
                        "deepseek-reasoner"
                    },
                    WebViewUrl = "https://chat.deepseek.com/"
                }
            },
            {
                "zhipu", new AiProviderInfo
                {
                    Name = "智谱AI (Zhipu/GLM)",
                    BaseUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions",
                    DefaultModel = "glm-4-flash",
                    Models = new List<string>
                    {
                        "glm-4-flash",
                        "glm-4-air",
                        "glm-4-airx",
                        "glm-4-long",
                        "glm-4-plus",
                        "glm-4"
                    },
                    WebViewUrl = "https://chatglm.cn"
                }
            },
            {
                "qwen", new AiProviderInfo
                {
                    Name = "通义千问 (Qwen/DashScope)",
                    BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                    DefaultModel = "qwen-turbo",
                    Models = new List<string>
                    {
                        "qwen-turbo",
                        "qwen-plus",
                        "qwen-max",
                        "qwen-long"
                    },
                    WebViewUrl = "https://tongyi.aliyun.com/qianwen"
                }
            },
            {
                "spark", new AiProviderInfo
                {
                    Name = "讯飞星火 (Spark)",
                    BaseUrl = "https://spark-api-open.xf-yun.com/v1/chat/completions",
                    DefaultModel = "4.0Ultra",
                    Models = new List<string>
                    {
                        "4.0Ultra",
                        "pro-128k",
                        "generalv3.5",
                        "generalv3",
                        "lite"
                    },
                    WebViewUrl = "https://xinghuo.xfyun.cn"
                }
            },
            {
                "wenxin", new AiProviderInfo
                {
                    Name = "文心一言 (ERNIE)",
                    BaseUrl = "https://qianfan.baidubce.com/v2/chat/completions",
                    DefaultModel = "ernie-4.0-8k-latest",
                    Models = new List<string>
                    {
                        "ernie-4.0-8k-latest",
                        "ernie-4.0-turbo-8k",
                        "ernie-3.5-8k",
                        "ernie-speed-8k",
                        "ernie-lite-8k"
                    },
                    WebViewUrl = "https://yiyan.baidu.com"
                }
            }
        }.ToImmutableDictionary();
    }

    public class AiProviderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
        /// <summary>该 provider 显式指定的模型（可选覆盖 DefaultModel）</summary>
        public string Model { get; set; } = string.Empty;
        /// <summary>该 provider 的 API Key（可选覆盖全局 Key）</summary>
        public string ApiKey { get; set; } = string.Empty;
        public List<string> Models { get; set; } = new();
        public string WebViewUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 解析后的 AI 端点配置（BaseUrl / Model / ApiKey 均已确定，可直接使用）。
    /// 由 <see cref="AiConfig.ResolveEndpoint"/> 根据 per-provider 覆盖配置、内置默认值与全局配置计算得出。
    /// </summary>
    public class AiEndpoint
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public AiEndpoint(string baseUrl, string model, string apiKey)
        {
            BaseUrl = baseUrl;
            Model = model;
            ApiKey = apiKey;
        }
    }
}
