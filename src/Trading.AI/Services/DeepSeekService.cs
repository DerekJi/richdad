using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.AI.Configuration;

namespace Trading.AI.Services;

/// <summary>
/// DeepSeek AI服务实现
/// </summary>
public class DeepSeekService : IDeepSeekService
{
    private readonly ILogger<DeepSeekService> _logger;
    private readonly DeepSeekSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, int> _dailyUsage = new();
    private readonly Dictionary<string, decimal> _monthlyCost = new();
    private readonly object _usageLock = new();

    public DeepSeekService(
        ILogger<DeepSeekService> logger,
        IOptions<DeepSeekSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_settings.Endpoint);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogInformation("DeepSeek服务已初始化 - Model: {Model}", _settings.ModelName);
    }

    public async Task<string> ChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("DeepSeek服务未启用");
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("DeepSeek ApiKey未配置");
        }

        if (IsRateLimitReached())
        {
            throw new InvalidOperationException("已达到每日调用限制");
        }

        var request = new
        {
            model = _settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = _settings.Temperature,
            max_tokens = _settings.MaxTokens
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
                    var assistantMessage = result.Choices[0].Message?.Content ?? string.Empty;

                    // 记录使用情况
                    RecordUsage(result.Usage?.PromptTokens ?? 0, result.Usage?.CompletionTokens ?? 0);

                    _logger.LogDebug("DeepSeek调用成功 - InputTokens: {Input}, OutputTokens: {Output}",
                        result.Usage?.PromptTokens,
                        result.Usage?.CompletionTokens);

                    return assistantMessage;
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

    public bool IsRateLimitReached()
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (_dailyUsage.TryGetValue(today, out var count))
            {
                return count >= _settings.MaxDailyRequests;
            }
            return false;
        }
    }

    public int GetTodayUsageCount()
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return _dailyUsage.TryGetValue(today, out var count) ? count : 0;
        }
    }

    public decimal GetEstimatedMonthlyCost()
    {
        lock (_usageLock)
        {
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            return _monthlyCost.TryGetValue(currentMonth, out var cost) ? cost : 0m;
        }
    }

    private void RecordUsage(int inputTokens, int outputTokens)
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");

            // 记录调用次数
            if (!_dailyUsage.ContainsKey(today))
                _dailyUsage[today] = 0;
            _dailyUsage[today]++;

            // 计算成本
            var cost = (inputTokens / 1_000_000m * _settings.CostPer1MInputTokens) +
                      (outputTokens / 1_000_000m * _settings.CostPer1MOutputTokens);

            if (!_monthlyCost.ContainsKey(currentMonth))
                _monthlyCost[currentMonth] = 0m;
            _monthlyCost[currentMonth] += cost;

            // 清理旧数据
            CleanupOldUsageData();
        }
    }

    private void CleanupOldUsageData()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var keysToRemove = _dailyUsage.Keys.Where(k => string.Compare(k, cutoffDate) < 0).ToList();
        foreach (var key in keysToRemove)
        {
            _dailyUsage.Remove(key);
        }

        var cutoffMonth = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM");
        var monthsToRemove = _monthlyCost.Keys.Where(k => string.Compare(k, cutoffMonth) < 0).ToList();
        foreach (var key in monthsToRemove)
        {
            _monthlyCost.Remove(key);
        }
    }

    // DeepSeek响应模型
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
