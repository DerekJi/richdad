using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Trading.AI.Configuration;

namespace Trading.AI.Services;

/// <summary>
/// Azure OpenAI服务实现
/// </summary>
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly AzureOpenAISettings _settings;
    private readonly AzureOpenAIClient _client;
    private readonly ChatClient _chatClient;
    private readonly Dictionary<string, int> _dailyUsage = new();
    private readonly Dictionary<string, int> _tokenUsage = new();
    private readonly object _usageLock = new();

    public AzureOpenAIService(
        ILogger<AzureOpenAIService> logger,
        IOptions<AzureOpenAISettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        if (string.IsNullOrEmpty(_settings.Endpoint) || string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI Endpoint 和 ApiKey 必须配置");
        }

        _client = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new AzureKeyCredential(_settings.ApiKey));

        _chatClient = _client.GetChatClient(_settings.DeploymentName);

        _logger.LogInformation("Azure OpenAI服务已初始化 - Endpoint: {Endpoint}, Deployment: {Deployment}",
            _settings.Endpoint, _settings.DeploymentName);
    }

    public async Task<string> ChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Azure OpenAI服务未启用");
        }

        if (IsRateLimitReached())
        {
            throw new InvalidOperationException($"已达到每日调用限制: {_settings.MaxDailyRequests}");
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = _settings.Temperature,
                MaxOutputTokenCount = _settings.MaxTokens
            };

            _logger.LogDebug("发送Azure OpenAI请求 - SystemPrompt长度: {SystemLength}, UserMessage长度: {UserLength}",
                systemPrompt.Length, userMessage.Length);

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("Azure OpenAI返回空响应");
            }

            var result = response.Value.Content[0].Text;

            // 记录使用量
            RecordUsage(response.Value.Usage);

            _logger.LogInformation("Azure OpenAI请求成功 - 输入Tokens: {InputTokens}, 输出Tokens: {OutputTokens}, 总计: {TotalTokens}",
                response.Value.Usage.InputTokenCount,
                response.Value.Usage.OutputTokenCount,
                response.Value.Usage.TotalTokenCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI请求失败");
            throw;
        }
    }

    public bool IsRateLimitReached()
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return _dailyUsage.TryGetValue(today, out var count) && count >= _settings.MaxDailyRequests;
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
            if (!_tokenUsage.TryGetValue(currentMonth, out var tokens))
            {
                return 0m;
            }

            // GPT-4o 价格估算: $5 per 1M tokens (平均输入输出)
            var costPerMillionTokens = 5m;
            return (decimal)tokens / 1_000_000m * costPerMillionTokens;
        }
    }

    private void RecordUsage(ChatTokenUsage usage)
    {
        lock (_usageLock)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");

            // 记录每日调用次数
            if (!_dailyUsage.ContainsKey(today))
            {
                _dailyUsage[today] = 0;
            }
            _dailyUsage[today]++;

            // 记录每月Token使用量
            if (!_tokenUsage.ContainsKey(currentMonth))
            {
                _tokenUsage[currentMonth] = 0;
            }
            _tokenUsage[currentMonth] += usage.TotalTokenCount;

            // 清理旧数据
            CleanupOldData();
        }
    }

    private void CleanupOldData()
    {
        var yesterday = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd");
        var lastMonth = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM");

        _dailyUsage.Keys.Where(k => string.Compare(k, yesterday) < 0).ToList()
            .ForEach(k => _dailyUsage.Remove(k));

        _tokenUsage.Keys.Where(k => string.Compare(k, lastMonth) < 0).ToList()
            .ForEach(k => _tokenUsage.Remove(k));
    }
}
