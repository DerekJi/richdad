using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.AI.Configuration;

namespace Trading.AI.Services;

/// <summary>
/// DeepSeek客户端适配器
/// </summary>
public class DeepSeekClientAdapter : IUnifiedAIClient
{
    private readonly ILogger<DeepSeekClientAdapter> _logger;
    private readonly DeepSeekSettings _settings;
    private readonly HttpClient _httpClient;

    public DeepSeekClientAdapter(
        ILogger<DeepSeekClientAdapter> logger,
        IOptions<DeepSeekSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek ApiKey 必须配置");
        }

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_settings.Endpoint);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogInformation("DeepSeek客户端已初始化");
    }

    public async Task<AIResponse> CompleteChatAsync(
        string systemPrompt,
        string userMessage,
        float temperature,
        int maxTokens,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        int retries = 0;
        while (retries < _settings.MaxRetries)
        {
            try
            {
                var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);

                if (result?.Choices?.Length > 0)
                {
                    return new AIResponse
                    {
                        Content = result.Choices[0].Message?.Content ?? string.Empty,
                        InputTokens = result.Usage?.PromptTokens ?? 0,
                        OutputTokens = result.Usage?.CompletionTokens ?? 0,
                        TotalTokens = (result.Usage?.PromptTokens ?? 0) + (result.Usage?.CompletionTokens ?? 0)
                    };
                }

                throw new InvalidOperationException("DeepSeek返回了空响应");
            }
            catch (HttpRequestException ex)
            {
                retries++;
                if (retries >= _settings.MaxRetries)
                {
                    _logger.LogError(ex, "DeepSeek调用失败，已达到最大重试次数");
                    throw;
                }

                _logger.LogWarning("DeepSeek调用失败，重试 {Retry}/{MaxRetries}: {Error}",
                    retries, _settings.MaxRetries, ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retries)), cancellationToken);
            }
        }

        throw new InvalidOperationException("DeepSeek调用失败");
    }

    private class DeepSeekResponse
    {
        public Choice[]? Choices { get; set; }
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
