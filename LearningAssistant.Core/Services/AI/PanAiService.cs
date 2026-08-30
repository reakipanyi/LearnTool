using System.Net.Http.Headers;
using System.Text;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LearningAssistant.Services.AI;

/// <summary>
/// 网盘 AI 分析专用服务，提供 ChatAsync 方法调用 AI 模型。
/// 与 IAIService 解耦，避免 Core 项目依赖 UI 项目的具体实现。
/// </summary>
public class PanAiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AiConfig _config;
    private readonly ILogger<PanAiService> _logger;

    public PanAiService(AiConfig config, ILogger<PanAiService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 60) };
    }

    /// <summary>调用 AI 模型的 Chat 接口，返回响应文本</summary>
    public async Task<string> ChatAsync(string prompt, string modelName, CancellationToken cancellationToken = default)
    {
        var endpoint = _config.ResolveEndpoint(modelName);
        if (string.IsNullOrWhiteSpace(endpoint.BaseUrl))
            throw new InvalidOperationException("未配置 AI 端点，模型：" + modelName);

        var requestBody = new
        {
            model = endpoint.Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", endpoint.ApiKey);

        _logger.LogDebug("发送 AI 请求到 {BaseUrl}，模型：{Model}", endpoint.BaseUrl, endpoint.Model);

        var response = await _httpClient.PostAsync(endpoint.BaseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonConvert.DeserializeAnonymousType(responseJson, new
        {
            choices = new[] { new { message = new { content = "" } } }
        });

        var result = parsed?.choices?.FirstOrDefault()?.message?.content ?? "";
        _logger.LogDebug("AI 响应长度：{Length} 字符", result.Length);
        return result;
    }

    public void Dispose() => _httpClient.Dispose();
}

